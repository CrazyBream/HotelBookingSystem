SELECT h.city, h.name AS hotel_name, r.room_number, rc.name AS category_name, rc.base_price, r.status
FROM hotel.rooms AS r
JOIN hotel.hotels AS h ON h.id = r.hotel_id
JOIN hotel.room_categories AS rc ON rc.id = r.category_id
WHERE h.is_active = true
ORDER BY h.city, rc.base_price;

SELECT h.name AS hotel_name, h.city, COUNT(b.id) AS bookings_count
FROM hotel.hotels AS h
LEFT JOIN hotel.rooms AS r ON r.hotel_id = h.id
LEFT JOIN hotel.bookings AS b ON b.room_id = r.id
GROUP BY h.id, h.name, h.city
ORDER BY bookings_count DESC, h.name;

SELECT g.id, g.first_name || ' ' || g.last_name AS full_name, g.email, g.is_banned
FROM hotel.guests AS g
LEFT JOIN hotel.bookings AS b ON b.guest_id = g.id
WHERE b.id IS NULL
ORDER BY g.id;

SELECT rc.name AS category_name, COUNT(b.id) AS bookings_count, ROUND(AVG(b.total_price), 2) AS avg_total
FROM hotel.room_categories AS rc
JOIN hotel.rooms AS r ON r.category_id = rc.id
JOIN hotel.bookings AS b ON b.room_id = r.id
GROUP BY rc.id, rc.name
HAVING COUNT(b.id) >= 2
ORDER BY avg_total DESC;

SELECT
    h.city,
    COUNT(b.id) AS total_bookings,
    COUNT(b.id) FILTER (WHERE b.status IN ('confirmed', 'checked_in', 'completed')) AS active_or_done,
    COUNT(b.id) FILTER (WHERE b.status IN ('cancelled', 'no_show')) AS lost_bookings
FROM hotel.hotels AS h
LEFT JOIN hotel.rooms AS r ON r.hotel_id = h.id
LEFT JOIN hotel.bookings AS b ON b.room_id = r.id
GROUP BY h.city
ORDER BY total_bookings DESC, h.city;

SELECT h.id, h.name, h.city
FROM hotel.hotels AS h
WHERE EXISTS (
    SELECT 1
    FROM hotel.rooms AS r
    JOIN hotel.room_amenities AS ra ON ra.room_id = r.id
    JOIN hotel.amenities AS a ON a.id = ra.amenity_id
    WHERE r.hotel_id = h.id
      AND a.name = 'Панорамний вид'
)
ORDER BY h.id;

SELECT h.name AS hotel_name, r.room_number, rc.name AS category_name, rc.base_price
FROM hotel.rooms AS r
JOIN hotel.hotels AS h ON h.id = r.hotel_id
JOIN hotel.room_categories AS rc ON rc.id = r.category_id
WHERE h.is_active = true
  AND r.status IN ('available', 'occupied')
  AND NOT EXISTS (
      SELECT 1
      FROM hotel.bookings AS b
      WHERE b.room_id = r.id
        AND b.status IN ('pending', 'confirmed', 'checked_in')
        AND daterange(b.check_in_date, b.check_out_date, '[)')
            && daterange('2026-12-01'::date, '2026-12-05'::date, '[)')
  )
ORDER BY h.name, r.room_number;

SELECT b.booking_code, g.first_name || ' ' || g.last_name AS guest_name, b.total_price, b.status
FROM hotel.bookings AS b
JOIN hotel.guests AS g ON g.id = b.guest_id
WHERE b.total_price > (
    SELECT AVG(total_price)
    FROM hotel.bookings
    WHERE status IN ('confirmed', 'checked_in', 'completed')
)
ORDER BY b.total_price DESC;

SELECT DISTINCT ON (g.id)
    g.id AS guest_id,
    g.first_name || ' ' || g.last_name AS guest_name,
    b.booking_code,
    b.check_in_date,
    b.status
FROM hotel.guests AS g
JOIN hotel.bookings AS b ON b.guest_id = g.id
ORDER BY g.id, b.check_in_date DESC, b.id DESC;

SELECT
    h.name AS hotel_name,
    COALESCE(SUM(b.total_price) FILTER (WHERE b.status IN ('confirmed', 'checked_in', 'completed')), 0.00) AS revenue,
    DENSE_RANK() OVER (
        ORDER BY COALESCE(SUM(b.total_price) FILTER (WHERE b.status IN ('confirmed', 'checked_in', 'completed')), 0.00) DESC
    ) AS revenue_rank
FROM hotel.hotels AS h
LEFT JOIN hotel.rooms AS r ON r.hotel_id = h.id
LEFT JOIN hotel.bookings AS b ON b.room_id = r.id
GROUP BY h.id, h.name
ORDER BY revenue_rank, h.name;

SELECT
    p.id,
    p.booking_id,
    p.paid_at,
    p.payment_type,
    p.amount,
    LAG(p.amount) OVER (ORDER BY p.paid_at, p.id) AS prev_amount,
    SUM(CASE WHEN p.payment_type = 'refund' THEN -p.amount ELSE p.amount END)
        OVER (ORDER BY p.paid_at, p.id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS running_net_revenue
FROM hotel.payments AS p
WHERE p.status = 'completed'
ORDER BY p.paid_at, p.id;

SELECT hotel_name, room_number, category_name, base_price
FROM (
    SELECT
        h.name AS hotel_name,
        r.room_number,
        rc.name AS category_name,
        rc.base_price,
        ROW_NUMBER() OVER (PARTITION BY h.id ORDER BY rc.base_price DESC, r.id ASC) AS rn
    FROM hotel.rooms AS r
    JOIN hotel.hotels AS h ON h.id = r.hotel_id
    JOIN hotel.room_categories AS rc ON rc.id = r.category_id
) AS ranked
WHERE rn = 1
ORDER BY base_price DESC;

WITH active_debt AS (
    SELECT booking_code, guest_full_name, hotel_name, status, total_price, paid_amount, balance_due
    FROM hotel.v_booking_summary
    WHERE status NOT IN ('cancelled', 'no_show')
      AND balance_due > 0
)
SELECT *
FROM active_debt
ORDER BY balance_due DESC;

WITH total_rooms AS (
    SELECT COUNT(*)::numeric AS cnt
    FROM hotel.rooms
)
SELECT
    a.name AS amenity_name,
    COUNT(ra.room_id) AS rooms_with_amenity,
    ROUND(COUNT(ra.room_id) * 100.0 / tr.cnt, 1) AS share_pct
FROM hotel.amenities AS a
LEFT JOIN hotel.room_amenities AS ra ON ra.amenity_id = a.id
CROSS JOIN total_rooms AS tr
GROUP BY a.id, a.name, tr.cnt
ORDER BY rooms_with_amenity DESC, a.name;

SELECT hotel_name, room_number, category_name, base_price, amenities
FROM hotel.v_room_catalog
WHERE city = 'Київ'
  AND hotel_is_active = true
ORDER BY base_price ASC;

SELECT *
FROM hotel.fn_find_available_rooms('Київ', '2026-12-05', '2026-12-08', 2::smallint);

INSERT INTO hotel.amenities (name, icon_code)
VALUES ('SPA-доступ', 'spa')
RETURNING id, name, icon_code;

INSERT INTO hotel.amenities (name, icon_code)
VALUES ('SPA-доступ', 'spa_vip')
ON CONFLICT (name) DO UPDATE
SET icon_code = EXCLUDED.icon_code
RETURNING id, name, icon_code;

UPDATE hotel.amenities
SET icon_code = 'wellness'
WHERE name = 'SPA-доступ'
RETURNING id, name, icon_code;

DELETE FROM hotel.amenities
WHERE name = 'SPA-доступ'
RETURNING id, name;