
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE SCHEMA hotel;

CREATE TABLE hotel.hotels (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    hotel_code  text NOT NULL UNIQUE CHECK (btrim(hotel_code) <> ''),
    name        text NOT NULL CHECK (btrim(name) <> ''),
    star_rating smallint NOT NULL CHECK (star_rating BETWEEN 1 AND 5),
    country     text NOT NULL CHECK (btrim(country) <> ''),
    city        text NOT NULL CHECK (btrim(city) <> ''),
    address     text NOT NULL CHECK (btrim(address) <> ''),
    phone       text NOT NULL CHECK (btrim(phone) <> ''),
    email       text NOT NULL UNIQUE CHECK (email ~* '^[^@\s]+@[^@\s]+\.[^@\s]+$'),
    description text,
    image_url   text,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE hotel.room_categories (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code        text NOT NULL UNIQUE CHECK (btrim(code) <> ''),
    name        text NOT NULL CHECK (btrim(name) <> ''),
    description text,
    max_guests  smallint NOT NULL CHECK (max_guests BETWEEN 1 AND 12),
    area_sqm    numeric(5,1) NOT NULL CHECK (area_sqm > 0),
    base_price  numeric(10,2) NOT NULL CHECK (base_price > 0)
);

CREATE TABLE hotel.rooms (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    hotel_id    integer NOT NULL
        REFERENCES hotel.hotels(id) ON DELETE RESTRICT,
    category_id integer NOT NULL
        REFERENCES hotel.room_categories(id) ON DELETE RESTRICT,
    room_number text NOT NULL CHECK (btrim(room_number) <> ''),
    floor       smallint NOT NULL CHECK (floor >= 0),
    status      text NOT NULL DEFAULT 'available'
        CHECK (status IN ('available', 'occupied', 'maintenance', 'inactive')),
    description text,
    image_url   text,
    CONSTRAINT rooms_hotel_number_uq UNIQUE (hotel_id, room_number)
);

CREATE TABLE hotel.amenities (
    id        integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name      text NOT NULL UNIQUE CHECK (btrim(name) <> ''),
    icon_code text
);

CREATE TABLE hotel.room_amenities (
    room_id    integer NOT NULL
        REFERENCES hotel.rooms(id) ON DELETE CASCADE,
    amenity_id integer NOT NULL
        REFERENCES hotel.amenities(id) ON DELETE RESTRICT,
    PRIMARY KEY (room_id, amenity_id)
);

CREATE TABLE hotel.guests (
    id              integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    first_name      text NOT NULL CHECK (btrim(first_name) <> ''),
    last_name       text NOT NULL CHECK (btrim(last_name) <> ''),
    email           text NOT NULL UNIQUE CHECK (email ~* '^[^@\s]+@[^@\s]+\.[^@\s]+$'),
    phone           text NOT NULL CHECK (btrim(phone) <> ''),
    document_type   text CHECK (document_type IN ('passport', 'id_card', 'driver_license')),
    document_number text CHECK (document_number IS NULL OR btrim(document_number) <> ''),
    password_hash   text,
    is_banned       boolean NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT guests_doc_pair_chk CHECK (
        (document_type IS NULL AND document_number IS NULL)
        OR (document_type IS NOT NULL AND document_number IS NOT NULL)
    ),
    CONSTRAINT guests_document_uq UNIQUE (document_type, document_number)
);

CREATE TABLE hotel.staff (
    id            integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    hotel_id      integer
        REFERENCES hotel.hotels(id) ON DELETE RESTRICT,
    first_name    text NOT NULL CHECK (btrim(first_name) <> ''),
    last_name     text NOT NULL CHECK (btrim(last_name) <> ''),
    email         text NOT NULL UNIQUE CHECK (email ~* '^[^@\s]+@[^@\s]+\.[^@\s]+$'),
    role_name     text NOT NULL CHECK (role_name IN ('admin', 'manager', 'receptionist', 'analyst')),
    password_hash text,
    is_active     boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE hotel.bookings (
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_code     text NOT NULL UNIQUE CHECK (btrim(booking_code) <> ''),
    guest_id         integer NOT NULL
        REFERENCES hotel.guests(id) ON DELETE RESTRICT,
    room_id          integer NOT NULL
        REFERENCES hotel.rooms(id) ON DELETE RESTRICT,
    check_in_date    date NOT NULL,
    check_out_date   date NOT NULL,
    guests_count     smallint NOT NULL CHECK (guests_count > 0),
    total_price      numeric(10,2) NOT NULL CHECK (total_price >= 0),
    status           text NOT NULL DEFAULT 'pending'
        CHECK (status IN ('pending', 'confirmed', 'checked_in', 'completed', 'cancelled', 'no_show')),
    special_requests text,
    created_at       timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at       timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT bookings_dates_chk CHECK (check_out_date > check_in_date),

    CONSTRAINT bookings_no_overlap_excl
        EXCLUDE USING gist (
            room_id WITH =,
            daterange(check_in_date, check_out_date, '[)') WITH &&
        )
        WHERE (status IN ('pending', 'confirmed', 'checked_in'))
);

CREATE TABLE hotel.stays (
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id       bigint NOT NULL UNIQUE
        REFERENCES hotel.bookings(id) ON DELETE RESTRICT,
    checked_in_by    integer NOT NULL
        REFERENCES hotel.staff(id) ON DELETE RESTRICT,
    checked_out_by   integer
        REFERENCES hotel.staff(id) ON DELETE RESTRICT,
    actual_check_in  timestamptz NOT NULL DEFAULT clock_timestamp(),
    actual_check_out timestamptz,
    room_key_card    text,
    notes            text,
    CONSTRAINT stays_checkout_after_checkin_chk CHECK (
        actual_check_out IS NULL OR actual_check_out >= actual_check_in
    )
);

CREATE TABLE hotel.payments (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id      bigint NOT NULL
        REFERENCES hotel.bookings(id) ON DELETE RESTRICT,
    processed_by    integer
        REFERENCES hotel.staff(id) ON DELETE RESTRICT,
    amount          numeric(10,2) NOT NULL CHECK (amount > 0),
    payment_method  text NOT NULL
        CHECK (payment_method IN ('card', 'cash', 'bank_transfer', 'online')),
    payment_type    text NOT NULL DEFAULT 'prepayment'
        CHECK (payment_type IN ('prepayment', 'final', 'refund')),
    status          text NOT NULL DEFAULT 'completed'
        CHECK (status IN ('pending', 'completed', 'failed', 'refunded')),
    transaction_ref text UNIQUE,
    paid_at         timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE hotel.booking_audit (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id bigint NOT NULL
        REFERENCES hotel.bookings(id) ON DELETE RESTRICT,
    old_status text,
    new_status text NOT NULL,
    changed_by text NOT NULL DEFAULT current_user,
    reason     text,
    changed_at timestamptz NOT NULL DEFAULT clock_timestamp()
);