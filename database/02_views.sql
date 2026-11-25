CREATE OR REPLACE VIEW hotel.v_room_catalog AS
SELECT
    r.id            AS room_id,
    r.room_number,
    r.floor,
    r.status        AS room_status,
    r.description   AS room_description,
    r.image_url     AS room_image_url,
    h.id            AS hotel_id,
    h.hotel_code,
    h.name          AS hotel_name,
    h.star_rating,
    h.country,
    h.city,
    h.address,
    h.is_active     AS hotel_is_active,
    rc.id           AS category_id,
    rc.code         AS category_code,
    rc.name         AS category_name,
    rc.max_guests,
    rc.area_sqm,
    rc.base_price,
    COALESCE(
        array_agg(a.name ORDER BY a.name) FILTER (WHERE a.id IS NOT NULL),
        ARRAY[]::text[]
    ) AS amenities
FROM hotel.rooms AS r
JOIN hotel.hotels AS h ON h.id = r.hotel_id
JOIN hotel.room_categories AS rc ON rc.id = r.category_id
LEFT JOIN hotel.room_amenities AS ra ON ra.room_id = r.id
LEFT JOIN hotel.amenities AS a ON a.id = ra.amenity_id
GROUP BY r.id, h.id, rc.id;

CREATE OR REPLACE VIEW hotel.v_booking_summary AS
SELECT
    b.id                                            AS booking_id,
    b.booking_code,
    b.status,
    b.check_in_date,
    b.check_out_date,
    (b.check_out_date - b.check_in_date)            AS nights_count,
    b.guests_count,
    b.total_price,
    COALESCE(pay.paid_amount, 0.00)::numeric(10,2)  AS paid_amount,
    (b.total_price - COALESCE(pay.paid_amount, 0.00))::numeric(10,2) AS balance_due,
    g.id                                            AS guest_id,
    g.first_name || ' ' || g.last_name              AS guest_full_name,
    g.email                                         AS guest_email,
    g.phone                                         AS guest_phone,
    h.id                                            AS hotel_id,
    h.name                                          AS hotel_name,
    h.city                                          AS hotel_city,
    r.id                                            AS room_id,
    r.room_number,
    rc.name                                         AS category_name,
    st.actual_check_in,
    st.actual_check_out,
    b.created_at
FROM hotel.bookings AS b
JOIN hotel.guests AS g ON g.id = b.guest_id
JOIN hotel.rooms AS r ON r.id = b.room_id
JOIN hotel.hotels AS h ON h.id = r.hotel_id
JOIN hotel.room_categories AS rc ON rc.id = r.category_id
LEFT JOIN hotel.stays AS st ON st.booking_id = b.id
LEFT JOIN (
    SELECT
        p.booking_id,
        SUM(
            CASE
                WHEN p.status = 'completed' AND p.payment_type IN ('prepayment', 'final') THEN p.amount
                WHEN p.status = 'completed' AND p.payment_type = 'refund' THEN -p.amount
                ELSE 0
            END
        ) AS paid_amount
    FROM hotel.payments AS p
    GROUP BY p.booking_id
) AS pay ON pay.booking_id = b.id;

CREATE OR REPLACE VIEW hotel.v_hotel_occupancy_stats AS
WITH room_stats AS (
    SELECT
        hotel_id,
        COUNT(*) AS total_rooms,
        COUNT(*) FILTER (WHERE status IN ('available', 'occupied')) AS operational_rooms
    FROM hotel.rooms
    GROUP BY hotel_id
),
booking_stats AS (
    SELECT
        r.hotel_id,
        COUNT(b.id) AS total_bookings,
        COUNT(b.id) FILTER (WHERE b.status IN ('confirmed', 'checked_in', 'completed')) AS confirmed_bookings,
        COUNT(b.id) FILTER (WHERE b.status IN ('cancelled', 'no_show')) AS cancelled_bookings,
        COALESCE(SUM(b.total_price) FILTER (WHERE b.status IN ('confirmed', 'checked_in', 'completed')), 0.00) AS expected_revenue,
        COALESCE(ROUND(AVG(b.check_out_date - b.check_in_date) FILTER (WHERE b.status IN ('confirmed', 'checked_in', 'completed')), 2), 0.00) AS avg_stay_nights
    FROM hotel.rooms AS r
    LEFT JOIN hotel.bookings AS b ON b.room_id = r.id
    GROUP BY r.hotel_id
),
payment_stats AS (
    SELECT
        r.hotel_id,
        COALESCE(SUM(
            CASE
                WHEN p.status = 'completed' AND p.payment_type IN ('prepayment', 'final') THEN p.amount
                WHEN p.status = 'completed' AND p.payment_type = 'refund' THEN -p.amount
                ELSE 0
            END
        ), 0.00) AS collected_revenue
    FROM hotel.rooms AS r
    LEFT JOIN hotel.bookings AS b ON b.room_id = r.id
    LEFT JOIN hotel.payments AS p ON p.booking_id = b.id
    GROUP BY r.hotel_id
)
SELECT
    h.id                                        AS hotel_id,
    h.hotel_code,
    h.name                                      AS hotel_name,
    h.city,
    h.star_rating,
    COALESCE(rs.total_rooms, 0)                 AS total_rooms,
    COALESCE(rs.operational_rooms, 0)           AS operational_rooms,
    COALESCE(bs.total_bookings, 0)              AS total_bookings,
    COALESCE(bs.confirmed_bookings, 0)          AS confirmed_bookings,
    COALESCE(bs.cancelled_bookings, 0)          AS cancelled_bookings,
    COALESCE(bs.avg_stay_nights, 0.00)          AS avg_stay_nights,
    COALESCE(bs.expected_revenue, 0.00)::numeric(12,2)  AS expected_revenue,
    COALESCE(ps.collected_revenue, 0.00)::numeric(12,2) AS collected_revenue
FROM hotel.hotels AS h
LEFT JOIN room_stats AS rs ON rs.hotel_id = h.id
LEFT JOIN booking_stats AS bs ON bs.hotel_id = h.id
LEFT JOIN payment_stats AS ps ON ps.hotel_id = h.id;