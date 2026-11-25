DELETE FROM hotel.payments WHERE transaction_ref LIKE 'PERF-TXN-%';
DELETE FROM hotel.bookings WHERE booking_code LIKE 'PERF-BK-%';

DROP INDEX IF EXISTS
    hotel.ix_bookings_guest_checkin,
    hotel.ix_payments_status_paid_at,
    hotel.ix_payments_booking_id,
    hotel.ix_rooms_hotel_id;

ALTER TABLE hotel.bookings DISABLE TRIGGER USER;

INSERT INTO hotel.bookings (
    booking_code, guest_id, room_id, check_in_date, check_out_date,
    guests_count, total_price, status
)
SELECT
    'PERF-BK-' || g,
    1 + (g % 6),
    (ARRAY[1,2,3,4,6,7,8,9,10,11])[1 + (g % 10)],
    DATE '2020-01-01' + (g % 2000),
    DATE '2020-01-01' + (g % 2000) + 2,
    1,
    2500.00 + (g % 5) * 500.00,
    'completed'
FROM generate_series(1, 50000) AS g;

ALTER TABLE hotel.bookings ENABLE TRIGGER USER;

WITH base_bookings AS (
    SELECT array_agg(id ORDER BY id) AS ids, count(*)::int AS cnt
    FROM hotel.bookings
    WHERE booking_code NOT LIKE 'PERF-BK-%'
)
INSERT INTO hotel.payments (
    booking_id, processed_by, amount, payment_method, payment_type,
    status, transaction_ref, paid_at
)
SELECT
    bb.ids[1 + (g % bb.cnt)],
    NULL,
    1000.00 + (g % 5000),
    (ARRAY['card','cash','bank_transfer','online'])[1 + (g % 4)],
    'final',
    CASE WHEN g % 10 = 0 THEN 'failed' ELSE 'completed' END,
    'PERF-TXN-' || g,
    TIMESTAMPTZ '2024-01-01 00:00:00+02' + g * INTERVAL '1 minute'
FROM generate_series(1, 100000) AS g
CROSS JOIN base_bookings AS bb;

ANALYZE hotel.bookings;
ANALYZE hotel.payments;

DO $$
DECLARE
    v_line text;
BEGIN
    RAISE NOTICE '=== Q1 BEFORE INDEX (bookings: guest_id = 1, ORDER BY check_in_date DESC LIMIT 20) ===';
    FOR v_line IN
        EXPLAIN (ANALYZE, BUFFERS)
        SELECT id, booking_code, room_id, check_in_date, total_price, status
        FROM hotel.bookings
        WHERE guest_id = 1
        ORDER BY check_in_date DESC
        LIMIT 20
    LOOP
        RAISE NOTICE '%', v_line;
    END LOOP;

    RAISE NOTICE '=== Q2 BEFORE INDEX (payments: status = completed, paid_at in 1 day) ===';
    FOR v_line IN
        EXPLAIN (ANALYZE, BUFFERS)
        SELECT COUNT(*), SUM(amount)
        FROM hotel.payments
        WHERE status = 'completed'
          AND paid_at >= '2024-02-01 00:00:00+02'
          AND paid_at <  '2024-02-02 00:00:00+02'
    LOOP
        RAISE NOTICE '%', v_line;
    END LOOP;
END;
$$;

CREATE INDEX ix_bookings_guest_checkin
    ON hotel.bookings (guest_id, check_in_date DESC);

CREATE INDEX ix_payments_status_paid_at
    ON hotel.payments (status, paid_at DESC)
    INCLUDE (amount, payment_type);

CREATE INDEX ix_payments_booking_id
    ON hotel.payments (booking_id);

CREATE INDEX ix_rooms_hotel_id
    ON hotel.rooms (hotel_id);

ANALYZE hotel.bookings;
ANALYZE hotel.payments;

DO $$
DECLARE
    v_line text;
BEGIN
    RAISE NOTICE '=== Q1 AFTER INDEX (bookings: guest_id = 1, ORDER BY check_in_date DESC LIMIT 20) ===';
    FOR v_line IN
        EXPLAIN (ANALYZE, BUFFERS)
        SELECT id, booking_code, room_id, check_in_date, total_price, status
        FROM hotel.bookings
        WHERE guest_id = 1
        ORDER BY check_in_date DESC
        LIMIT 20
    LOOP
        RAISE NOTICE '%', v_line;
    END LOOP;

    RAISE NOTICE '=== Q2 AFTER INDEX (payments: status = completed, paid_at in 1 day) ===';
    FOR v_line IN
        EXPLAIN (ANALYZE, BUFFERS)
        SELECT COUNT(*), SUM(amount)
        FROM hotel.payments
        WHERE status = 'completed'
          AND paid_at >= '2024-02-01 00:00:00+02'
          AND paid_at <  '2024-02-02 00:00:00+02'
    LOOP
        RAISE NOTICE '%', v_line;
    END LOOP;

    RAISE NOTICE '=== INDEX SIZES (at 50k bookings / 100k payments) ===';
    RAISE NOTICE 'ix_bookings_guest_checkin: %', pg_size_pretty(pg_relation_size('hotel.ix_bookings_guest_checkin'));
    RAISE NOTICE 'ix_payments_status_paid_at: %', pg_size_pretty(pg_relation_size('hotel.ix_payments_status_paid_at'));
    RAISE NOTICE 'ix_payments_booking_id: %', pg_size_pretty(pg_relation_size('hotel.ix_payments_booking_id'));
    RAISE NOTICE 'ix_rooms_hotel_id: %', pg_size_pretty(pg_relation_size('hotel.ix_rooms_hotel_id'));
END;
$$;

DELETE FROM hotel.payments WHERE transaction_ref LIKE 'PERF-TXN-%';
DELETE FROM hotel.bookings WHERE booking_code LIKE 'PERF-BK-%';

ANALYZE hotel.bookings;
ANALYZE hotel.payments;

SELECT
    (SELECT COUNT(*) FROM hotel.bookings) AS bookings_after_cleanup,
    (SELECT COUNT(*) FROM hotel.payments) AS payments_after_cleanup,
    (SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'hotel' AND indexname LIKE 'ix_%') AS custom_indexes_count;