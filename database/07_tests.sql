BEGIN;

DO $$
DECLARE
    v_booking_id   bigint;
    v_booking_code text;
    v_total_price  numeric;
    v_stay_id      bigint;
    v_cnt_before   bigint;
    v_cnt_after    bigint;
    v_status       text;
BEGIN
    CALL hotel.sp_create_booking(1, 11, '2026-12-20', '2026-12-23', 2::smallint, 'Тест T01', v_booking_id, v_booking_code, v_total_price);
    IF v_booking_id IS NULL OR v_total_price <> 7500.00 THEN
        RAISE EXCEPTION 'T01 FAILED';
    END IF;
    RAISE NOTICE 'T01 PASSED: sp_create_booking -> id=%, code=%, total=%', v_booking_id, v_booking_code, v_total_price;

    BEGIN
        INSERT INTO hotel.rooms (hotel_id, category_id, room_number, floor)
        VALUES (1, 1, '101', 1);
        RAISE EXCEPTION 'T02 FAILED: дублікат номера дозволено';
    EXCEPTION WHEN unique_violation THEN
        RAISE NOTICE 'T02 PASSED (BR01): дублікат (hotel_id, room_number) відхилено';
    END;

    BEGIN
        CALL hotel.sp_create_booking(1, 11, '2026-12-25', '2026-12-25', 1::smallint, NULL, v_booking_id, v_booking_code, v_total_price);
        RAISE EXCEPTION 'T03 FAILED: некоректні дати дозволено';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'T03 PASSED (BR02): check_out_date <= check_in_date відхилено';
    END;

    BEGIN
        CALL hotel.sp_create_booking(2, 11, '2026-12-21', '2026-12-24', 1::smallint, NULL, v_booking_id, v_booking_code, v_total_price);
        RAISE EXCEPTION 'T04 FAILED: перетин дат дозволено';
    EXCEPTION WHEN exclusion_violation THEN
        RAISE NOTICE 'T04 PASSED (BR03): перетин дат бронювання відхилено EXCLUDE';
    END;

    BEGIN
        CALL hotel.sp_create_booking(1, 1, '2026-12-10', '2026-12-12', 3::smallint, NULL, v_booking_id, v_booking_code, v_total_price);
        RAISE EXCEPTION 'T05 FAILED: перевищення місткості дозволено';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'T05 PASSED (BR04): перевищення max_guests відхилено тригером';
    END;

    BEGIN
        UPDATE hotel.bookings SET status = 'pending' WHERE id = 1;
        RAISE EXCEPTION 'T06 FAILED: заборонений перехід статусу дозволено';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'T06 PASSED (BR05): перехід completed -> pending відхилено тригером';
    END;

    BEGIN
        CALL hotel.sp_create_booking(7, 11, '2027-01-05', '2027-01-07', 1::smallint, NULL, v_booking_id, v_booking_code, v_total_price);
        RAISE EXCEPTION 'T07 FAILED: бронювання заблокованим гостем дозволено';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'T07 PASSED: заблокований гість (is_banned=true) відхилений';
    END;

    BEGIN
        DELETE FROM hotel.hotels WHERE id = 1;
        RAISE EXCEPTION 'T08 FAILED: видалення готелю з історією дозволено';
    EXCEPTION WHEN foreign_key_violation THEN
        RAISE NOTICE 'T08 PASSED (BR07): ON DELETE RESTRICT захистив історію';
    END;

    BEGIN
        INSERT INTO hotel.payments (booking_id, amount, payment_method)
        VALUES (9, -100.00, 'card');
        RAISE EXCEPTION 'T09 FAILED: від’ємний платіж дозволено';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'T09 PASSED (BR09): amount <= 0 відхилено CHECK';
    END;

    SELECT COUNT(*) INTO v_cnt_before FROM hotel.booking_audit WHERE booking_id = 9;
    BEGIN
        UPDATE hotel.bookings SET status = 'cancelled' WHERE id = 9;
        RAISE EXCEPTION 'force_rollback';
    EXCEPTION WHEN OTHERS THEN
        SELECT status INTO v_status FROM hotel.bookings WHERE id = 9;
        SELECT COUNT(*) INTO v_cnt_after FROM hotel.booking_audit WHERE booking_id = 9;
        IF v_status <> 'confirmed' OR v_cnt_after <> v_cnt_before THEN
            RAISE EXCEPTION 'T10 FAILED: відкат не відновив стан';
        END IF;
        RAISE NOTICE 'T10 PASSED (BR06): після ROLLBACK статус=% і аудит не змінився (%)', v_status, v_cnt_after;
    END;

    CALL hotel.sp_check_in_guest(9, 2, 'KEY-T11', 'Тестове заселення', v_stay_id);
    CALL hotel.sp_check_out_guest(9, 2, 'Тестове виселення');
    SELECT status INTO v_status FROM hotel.bookings WHERE id = 9;
    IF v_status <> 'completed' THEN
        RAISE EXCEPTION 'T11 FAILED: статус=%', v_status;
    END IF;
    RAISE NOTICE 'T11 PASSED: повний цикл sp_check_in_guest -> sp_check_out_guest (stay_id=%)', v_stay_id;

    BEGIN
        SET LOCAL ROLE hotel_analyst;
        INSERT INTO hotel.hotels (hotel_code, name, star_rating, country, city, address, phone, email)
        VALUES ('TEST-99', 'Hack Hotel', 3, 'UA', 'Київ', 'вул. 1', '+38000', 'hack@test.ua');
        RAISE EXCEPTION 'T12a FAILED: аналітик зміг виконати INSERT';
    EXCEPTION WHEN insufficient_privilege THEN
        RESET ROLE;
        RAISE NOTICE 'T12a PASSED (NFR01): hotel_analyst отримав відмову на INSERT';
    END;

    BEGIN
        SET LOCAL ROLE hotel_receptionist;
        CALL hotel.sp_create_booking(3, 11, '2027-02-01', '2027-02-03', 2::smallint, 'Від рецепціоніста', v_booking_id, v_booking_code, v_total_price);
        DELETE FROM hotel.booking_audit;
        RAISE EXCEPTION 'T12b FAILED: рецепціоніст зміг видалити аудит';
    EXCEPTION WHEN insufficient_privilege THEN
        RESET ROLE;
        RAISE NOTICE 'T12b PASSED: hotel_receptionist створив бронювання (%), але отримав відмову на DELETE booking_audit', v_booking_code;
    END;
END;
$$;

ROLLBACK;