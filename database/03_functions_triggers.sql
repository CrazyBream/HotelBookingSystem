CREATE SEQUENCE IF NOT EXISTS hotel.booking_code_seq START 1;

CREATE OR REPLACE FUNCTION hotel.fn_calculate_stay_price(
    p_room_id   integer,
    p_check_in  date,
    p_check_out date
)
RETURNS numeric
LANGUAGE sql
STABLE
AS $$
    SELECT (rc.base_price * (p_check_out - p_check_in))::numeric(10,2)
    FROM hotel.rooms AS r
    JOIN hotel.room_categories AS rc ON rc.id = r.category_id
    WHERE r.id = p_room_id
      AND p_check_out > p_check_in;
$$;

CREATE OR REPLACE FUNCTION hotel.fn_find_available_rooms(
    p_city      text,
    p_check_in  date,
    p_check_out date,
    p_guests    smallint DEFAULT 1
)
RETURNS TABLE (
    room_id          integer,
    hotel_id         integer,
    hotel_name       text,
    city             text,
    star_rating      smallint,
    room_number      text,
    category_name    text,
    max_guests       smallint,
    base_price       numeric(10,2),
    total_stay_price numeric(10,2)
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        r.id AS room_id,
        h.id AS hotel_id,
        h.name AS hotel_name,
        h.city,
        h.star_rating,
        r.room_number,
        rc.name AS category_name,
        rc.max_guests,
        rc.base_price,
        (rc.base_price * (p_check_out - p_check_in))::numeric(10,2) AS total_stay_price
    FROM hotel.rooms AS r
    JOIN hotel.hotels AS h ON h.id = r.hotel_id
    JOIN hotel.room_categories AS rc ON rc.id = r.category_id
    WHERE p_check_out > p_check_in
      AND h.is_active = true
      AND r.status IN ('available', 'occupied')
      AND rc.max_guests >= p_guests
      AND (p_city IS NULL OR btrim(p_city) = '' OR lower(h.city) = lower(btrim(p_city)))
      AND NOT EXISTS (
          SELECT 1
          FROM hotel.bookings AS b
          WHERE b.room_id = r.id
            AND b.status IN ('pending', 'confirmed', 'checked_in')
            AND daterange(b.check_in_date, b.check_out_date, '[)')
                && daterange(p_check_in, p_check_out, '[)')
      )
    ORDER BY h.star_rating DESC, total_stay_price ASC, r.room_number ASC;
$$;

CREATE OR REPLACE FUNCTION hotel.trg_fn_validate_booking()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_max_guests   smallint;
    v_room_status  text;
    v_hotel_active boolean;
    v_guest_banned boolean;
    v_allowed      boolean := false;
BEGIN
    SELECT rc.max_guests, r.status, h.is_active
    INTO v_max_guests, v_room_status, v_hotel_active
    FROM hotel.rooms AS r
    JOIN hotel.room_categories AS rc ON rc.id = r.category_id
    JOIN hotel.hotels AS h ON h.id = r.hotel_id
    WHERE r.id = NEW.room_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Номер з id=% не знайдено', NEW.room_id
            USING ERRCODE = 'foreign_key_violation';
    END IF;

    IF NEW.guests_count > v_max_guests THEN
        RAISE EXCEPTION 'Кількість гостей (%) перевищує місткість номера (%)',
            NEW.guests_count, v_max_guests
            USING ERRCODE = 'check_violation';
    END IF;

    IF TG_OP = 'INSERT' THEN
        IF NOT v_hotel_active THEN
            RAISE EXCEPTION 'Готель неактивний, створення бронювання неможливе'
                USING ERRCODE = 'check_violation';
        END IF;

        IF v_room_status IN ('maintenance', 'inactive') THEN
            RAISE EXCEPTION 'Номер id=% недоступний для бронювання (статус: %)',
                NEW.room_id, v_room_status
                USING ERRCODE = 'check_violation';
        END IF;

        SELECT is_banned INTO v_guest_banned
        FROM hotel.guests
        WHERE id = NEW.guest_id;

        IF v_guest_banned THEN
            RAISE EXCEPTION 'Гість id=% заблокований і не може створювати бронювання',
                NEW.guest_id
                USING ERRCODE = 'check_violation';
        END IF;
    END IF;

    IF TG_OP = 'UPDATE' THEN
        NEW.updated_at := clock_timestamp();

        IF OLD.status IS DISTINCT FROM NEW.status THEN
            v_allowed := (
                (OLD.status = 'pending'    AND NEW.status IN ('confirmed', 'cancelled', 'no_show'))
                OR (OLD.status = 'confirmed'  AND NEW.status IN ('checked_in', 'cancelled', 'no_show'))
                OR (OLD.status = 'checked_in' AND NEW.status = 'completed')
            );

            IF NOT v_allowed THEN
                RAISE EXCEPTION 'Недопустимий перехід статусу бронювання: % -> %',
                    OLD.status, NEW.status
                    USING ERRCODE = 'check_violation';
            END IF;
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_bookings_validate_before ON hotel.bookings;
CREATE TRIGGER trg_bookings_validate_before
BEFORE INSERT OR UPDATE ON hotel.bookings
FOR EACH ROW
EXECUTE FUNCTION hotel.trg_fn_validate_booking();

CREATE OR REPLACE FUNCTION hotel.trg_fn_audit_booking_status()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO hotel.booking_audit (booking_id, old_status, new_status, reason)
        VALUES (NEW.id, NULL, NEW.status, 'Створення бронювання');
    ELSIF OLD.status IS DISTINCT FROM NEW.status THEN
        INSERT INTO hotel.booking_audit (booking_id, old_status, new_status, reason)
        VALUES (NEW.id, OLD.status, NEW.status, 'Зміна статусу бронювання');
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_bookings_status_audit ON hotel.bookings;
CREATE TRIGGER trg_bookings_status_audit
AFTER INSERT OR UPDATE OF status ON hotel.bookings
FOR EACH ROW
EXECUTE FUNCTION hotel.trg_fn_audit_booking_status();

CREATE OR REPLACE PROCEDURE hotel.sp_create_booking(
    IN  p_guest_id         integer,
    IN  p_room_id          integer,
    IN  p_check_in         date,
    IN  p_check_out        date,
    IN  p_guests_count     smallint,
    IN  p_special_requests text,
    OUT p_booking_id       bigint,
    OUT p_booking_code     text,
    OUT p_total_price      numeric
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_check_out <= p_check_in THEN
        RAISE EXCEPTION 'Дата виїзду (%) має бути пізнішою за дату заїзду (%)',
            p_check_out, p_check_in
            USING ERRCODE = 'check_violation';
    END IF;

    p_total_price := hotel.fn_calculate_stay_price(p_room_id, p_check_in, p_check_out);
    IF p_total_price IS NULL THEN
        RAISE EXCEPTION 'Неможливо розрахувати вартість: номер id=% не знайдено', p_room_id
            USING ERRCODE = 'foreign_key_violation';
    END IF;

    p_booking_code := 'BK-' || to_char(clock_timestamp(), 'YYYYMMDD') || '-'
                      || lpad(nextval('hotel.booking_code_seq')::text, 5, '0');

    INSERT INTO hotel.bookings (
        booking_code, guest_id, room_id,
        check_in_date, check_out_date,
        guests_count, total_price, status, special_requests
    )
    VALUES (
        p_booking_code, p_guest_id, p_room_id,
        p_check_in, p_check_out,
        p_guests_count, p_total_price, 'pending', p_special_requests
    )
    RETURNING id INTO p_booking_id;
END;
$$;

CREATE OR REPLACE PROCEDURE hotel.sp_check_in_guest(
    IN  p_booking_id    bigint,
    IN  p_staff_id      integer,
    IN  p_room_key_card text,
    IN  p_notes         text,
    OUT p_stay_id       bigint
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_room_id      integer;
    v_status       text;
    v_staff_active boolean;
BEGIN
    SELECT is_active INTO v_staff_active
    FROM hotel.staff
    WHERE id = p_staff_id;

    IF NOT FOUND OR NOT v_staff_active THEN
        RAISE EXCEPTION 'Працівника id=% не знайдено або він неактивний', p_staff_id
            USING ERRCODE = 'check_violation';
    END IF;

    SELECT room_id, status
    INTO v_room_id, v_status
    FROM hotel.bookings
    WHERE id = p_booking_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Бронювання id=% не знайдено', p_booking_id
            USING ERRCODE = 'no_data_found';
    END IF;

    IF v_status <> 'confirmed' THEN
        RAISE EXCEPTION 'Заселення можливе лише для підтвердженого бронювання (поточний статус: %)',
            v_status
            USING ERRCODE = 'check_violation';
    END IF;

    UPDATE hotel.bookings
    SET status = 'checked_in'
    WHERE id = p_booking_id;

    UPDATE hotel.rooms
    SET status = 'occupied'
    WHERE id = v_room_id;

    INSERT INTO hotel.stays (booking_id, checked_in_by, room_key_card, notes)
    VALUES (p_booking_id, p_staff_id, p_room_key_card, p_notes)
    RETURNING id INTO p_stay_id;
END;
$$;

CREATE OR REPLACE PROCEDURE hotel.sp_check_out_guest(
    IN p_booking_id bigint,
    IN p_staff_id   integer,
    IN p_notes      text DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_room_id      integer;
    v_status       text;
    v_staff_active boolean;
BEGIN
    SELECT is_active INTO v_staff_active
    FROM hotel.staff
    WHERE id = p_staff_id;

    IF NOT FOUND OR NOT v_staff_active THEN
        RAISE EXCEPTION 'Працівника id=% не знайдено або він неактивний', p_staff_id
            USING ERRCODE = 'check_violation';
    END IF;

    SELECT room_id, status
    INTO v_room_id, v_status
    FROM hotel.bookings
    WHERE id = p_booking_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Бронювання id=% не знайдено', p_booking_id
            USING ERRCODE = 'no_data_found';
    END IF;

    IF v_status <> 'checked_in' THEN
        RAISE EXCEPTION 'Виселення можливе лише для активного заселення (поточний статус: %)',
            v_status
            USING ERRCODE = 'check_violation';
    END IF;

    UPDATE hotel.stays
    SET actual_check_out = clock_timestamp(),
        checked_out_by   = p_staff_id,
        notes            = COALESCE(p_notes, notes)
    WHERE booking_id = p_booking_id;

    UPDATE hotel.bookings
    SET status = 'completed'
    WHERE id = p_booking_id;

    UPDATE hotel.rooms
    SET status = 'available'
    WHERE id = v_room_id;
END;
$$;