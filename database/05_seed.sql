TRUNCATE TABLE
    hotel.booking_audit,
    hotel.payments,
    hotel.stays,
    hotel.bookings,
    hotel.room_amenities,
    hotel.amenities,
    hotel.rooms,
    hotel.room_categories,
    hotel.staff,
    hotel.guests,
    hotel.hotels
RESTART IDENTITY CASCADE;

SELECT setval('hotel.booking_code_seq', 1, false);

INSERT INTO hotel.hotels (
    hotel_code, name, star_rating, country, city, address, phone, email, description, image_url, is_active
)
VALUES
    ('HTL-KYIV-01', 'Grand Kyiv Palace', 5, 'Україна', 'Київ', 'вул. Хрещатик, 15', '+380441112233', 'info@grandkyiv.ua',
     'П’ятизірковий готель у центрі столиці зі SPA-зоною та конференц-залами.', '/images/hotels/kyiv-palace.jpg', true),
    ('HTL-LVIV-01', 'Lviv Heritage Boutique', 4, 'Україна', 'Львів', 'пл. Ринок, 24', '+380322223344', 'booking@lvivheritage.ua',
     'Бутик-готель в історичному серці Львова з панорамною терасою.', '/images/hotels/lviv-heritage.jpg', true),
    ('HTL-ODES-01', 'Black Sea Pearl', 4, 'Україна', 'Одеса', 'Французький бульвар, 60', '+380483334455', 'stay@blackseapearl.ua',
     'Курортний готель біля узбережжя Чорного моря з власним пляжем.', '/images/hotels/odesa-pearl.jpg', true),
    ('HTL-BUKV-01', 'Carpathian Pine Lodge', 3, 'Україна', 'Буковель', 'вул. Карпатська, 8', '+380344445566', 'hello@carpathianpine.ua',
     'Новий гірський готель поруч із підйомниками (ще без бронювань).', '/images/hotels/bukovel-lodge.jpg', true),
    ('HTL-DNIP-01', 'Dnipro River Inn', 2, 'Україна', 'Дніпро', 'Січеславська Набережна, 12', '+380565556677', 'archive@dniproriver.ua',
     'Готель тимчасово зачинений на реконструкцію.', '/images/hotels/dnipro-inn.jpg', false);

INSERT INTO hotel.room_categories (code, name, description, max_guests, area_sqm, base_price)
VALUES
    ('STD-1', 'Standard Single', 'Компактний одномісний номер для ділових поїздок.', 1, 18.0, 1800.00),
    ('STD-2', 'Standard Double', 'Класичний двомісний номер з одним двоспальним або двома ліжками.', 2, 24.5, 2500.00),
    ('DLX-3', 'Deluxe Family',   'Просторий покращений номер із зоною відпочинку.', 3, 35.0, 3900.00),
    ('STE-4', 'Executive Suite', 'Двокімнатний люкс підвищеного комфорту з вітальнею.', 4, 58.0, 6800.00);

INSERT INTO hotel.rooms (hotel_id, category_id, room_number, floor, status, description, image_url)
VALUES
    (1, 1, '101', 1, 'available',   'Тихий одномісний номер із видом у двір.', '/images/rooms/std1.jpg'),
    (1, 2, '102', 1, 'available',   'Світлий двомісний номер біля ліфта.', '/images/rooms/std2.jpg'),
    (1, 3, '201', 2, 'available',   'Покращений номер із балконом на Хрещатик.', '/images/rooms/dlx3.jpg'),
    (1, 4, '301', 3, 'available',   'Представницький люкс із панорамними вікнами.', '/images/rooms/ste4.jpg'),
    (1, 2, '302', 3, 'maintenance', 'Планова заміна системи кондиціонування.', '/images/rooms/std2.jpg'),
    (2, 2, '101', 1, 'available',   'Затишний номер в австрійському стилі.', '/images/rooms/lviv-std.jpg'),
    (2, 3, '201', 2, 'available',   'Делюкс із видом на Ратушу.', '/images/rooms/lviv-dlx.jpg'),
    (2, 4, '301', 3, 'available',   'Люкс із каміном та терасою.', '/images/rooms/lviv-ste.jpg'),
    (3, 2, '105', 1, 'available',   'Номер із виходом до саду.', '/images/rooms/odesa-std.jpg'),
    (3, 3, '205', 2, 'available',   'Номер із прямим видом на море.', '/images/rooms/odesa-dlx.jpg'),
    (4, 2, '101', 1, 'available',   'Дерев’яний інтер’єр, вид на схили.', '/images/rooms/bukovel-std.jpg'),
    (5, 1, '101', 1, 'inactive',    'Виведений з експлуатації на період ремонту.', NULL);

INSERT INTO hotel.amenities (name, icon_code)
VALUES
    ('Wi-Fi', 'wifi'),
    ('Кондиціонер', 'ac'),
    ('Балкон', 'balcony'),
    ('Мінібар', 'minibar'),
    ('Сейф', 'safe'),
    ('Панорамний вид', 'view');

INSERT INTO hotel.room_amenities (room_id, amenity_id)
VALUES
    (1, 1), (1, 2), (1, 5),
    (2, 1), (2, 2), (2, 5),
    (3, 1), (3, 2), (3, 3), (3, 4), (3, 5),
    (4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6),
    (5, 1), (5, 2),
    (6, 1), (6, 2),
    (7, 1), (7, 2), (7, 3), (7, 6),
    (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 6),
    (9, 1), (9, 2), (9, 3),
    (10, 1), (10, 2), (10, 3), (10, 4), (10, 6),
    (11, 1), (11, 3), (11, 6);

INSERT INTO hotel.guests (
    first_name, last_name, email, phone, document_type, document_number, is_banned
)
VALUES
    ('Олександр', 'Коваленко',  'o.kovalenko@example.ua',  '+380671112233', 'passport',       'AA123456',  false),
    ('Марія',     'Шевченко',   'm.shevchenko@example.ua', '+380502223344', 'id_card',        '001234567', false),
    ('Дмитро',    'Бондаренко', 'd.bondarenko@example.ua', '+380933334455', 'passport',       'BB654321',  false),
    ('Ірина',     'Ткаченко',   'i.tkachenko@example.ua',  '+380664445566', NULL,             NULL,        false),
    ('Андрій',    'Мельник',    'a.melnyk@example.ua',     '+380975556677', 'driver_license', 'DL998877',  false),
    ('Софія',     'Кравчук',    's.kravchuk@example.ua',   '+380686667788', 'passport',       'CC112233',  false),
    ('Віктор',    'Сидоренко',  'v.sydorenko@example.ua',  '+380997778899', 'passport',       'DD000001',  true);

INSERT INTO hotel.staff (hotel_id, first_name, last_name, email, role_name, is_active)
VALUES
    (NULL, 'Ігор',     'Савченко',  'admin@hotelchain.ua',      'admin',        true),
    (1,    'Олена',    'Мороз',     'kyiv.desk@hotelchain.ua',  'receptionist', true),
    (2,    'Тарас',    'Лисенко',   'lviv.mgr@hotelchain.ua',   'manager',      true),
    (3,    'Наталія',  'Гончар',    'odesa.desk@hotelchain.ua', 'receptionist', true),
    (NULL, 'Катерина', 'Романюк',   'analyst@hotelchain.ua',    'analyst',      true);

INSERT INTO hotel.bookings (
    booking_code, guest_id, room_id, check_in_date, check_out_date,
    guests_count, total_price, status, special_requests
)
VALUES
    ('BK-20261001-00001', 1, 2,  '2026-10-05', '2026-10-08', 2,  7500.00, 'completed', 'Тихий поверх, два окремі ліжка'),
    ('BK-20261010-00002', 2, 4,  '2026-10-12', '2026-10-15', 4, 20400.00, 'completed', 'Дитяче ліжечко та пізній виїзд'),
    ('BK-20261015-00003', 3, 6,  '2026-10-18', '2026-10-20', 2,  5000.00, 'completed', NULL),
    ('BK-20261020-00004', 1, 7,  '2026-10-22', '2026-10-25', 2, 11700.00, 'cancelled', 'Скасовано гостем за 48 годин'),
    ('BK-20261101-00005', 5, 9,  '2026-11-05', '2026-11-06', 1,  2500.00, 'no_show',   NULL),
    ('BK-20261110-00006', 2, 10, '2026-11-12', '2026-11-16', 3, 15600.00, 'completed', 'Вид на море'),
    ('BK-20261120-00007', 4, 3,  '2026-11-24', '2026-11-28', 2, 15600.00, 'confirmed', 'Ранній заїзд'),
    ('BK-20261122-00008', 3, 8,  '2026-11-24', '2026-11-27', 3, 20400.00, 'confirmed', 'Трансфер із вокзалу'),
    ('BK-20261125-00009', 1, 1,  '2026-12-01', '2026-12-04', 1,  5400.00, 'confirmed', NULL),
    ('BK-20261125-00010', 5, 2,  '2026-12-05', '2026-12-08', 2,  7500.00, 'pending',   'Потрібен рахунок для відрядження'),
    ('BK-20261125-00011', 2, 2,  '2026-12-08', '2026-12-10', 2,  5000.00, 'confirmed', NULL);

INSERT INTO hotel.stays (
    booking_id, checked_in_by, checked_out_by, actual_check_in, actual_check_out, room_key_card, notes
)
VALUES
    (1, 2, 2, '2026-10-05 14:15:00+03', '2026-10-08 11:40:00+03', 'KEY-K102', 'Зауважень немає'),
    (2, 2, 2, '2026-10-12 15:00:00+03', '2026-10-15 12:00:00+03', 'KEY-K301', 'VIP-обслуговування'),
    (3, 3, 3, '2026-10-18 13:50:00+03', '2026-10-20 11:10:00+03', 'KEY-L101', NULL),
    (6, 4, 4, '2026-11-12 14:00:00+02', '2026-11-16 11:30:00+02', 'KEY-O205', 'Пізній сніданок');

DO $$
DECLARE
    v_stay_id bigint;
BEGIN
    CALL hotel.sp_check_in_guest(7, 2, 'KEY-K201', 'Гість заселився вчасно', v_stay_id);
    CALL hotel.sp_check_in_guest(8, 3, 'KEY-L301', 'Видано два ключі', v_stay_id);
END;
$$;

INSERT INTO hotel.payments (
    booking_id, processed_by, amount, payment_method, payment_type, status, transaction_ref, paid_at
)
VALUES
    (1, NULL,  2500.00, 'online',        'prepayment', 'completed', 'TXN-20261001-01', '2026-10-01 10:20:00+03'),
    (1, 2,     5000.00, 'card',          'final',      'completed', 'TXN-20261005-02', '2026-10-05 14:10:00+03'),
    (2, NULL, 20400.00, 'bank_transfer', 'final',      'completed', 'TXN-20261010-03', '2026-10-10 16:00:00+03'),
    (3, 3,     5000.00, 'card',          'final',      'completed', 'TXN-20261018-04', '2026-10-18 13:45:00+03'),
    (4, NULL,  3900.00, 'online',        'prepayment', 'completed', 'TXN-20261020-05', '2026-10-20 09:00:00+03'),
    (4, 3,     3900.00, 'online',        'refund',     'completed', 'TXN-20261020-06', '2026-10-20 18:30:00+03'),
    (5, NULL,  2500.00, 'online',        'prepayment', 'completed', 'TXN-20261101-07', '2026-11-01 11:15:00+02'),
    (6, 4,    15600.00, 'card',          'final',      'completed', 'TXN-20261112-08', '2026-11-12 13:55:00+02'),
    (7, NULL,  5000.00, 'online',        'prepayment', 'completed', 'TXN-20261120-09', '2026-11-20 12:00:00+02'),
    (8, NULL, 20400.00, 'online',        'final',      'completed', 'TXN-20261122-10', '2026-11-22 15:10:00+02'),
    (9, NULL,  1800.00, 'online',        'prepayment', 'failed',    'TXN-20261125-11', '2026-11-25 08:00:00+02'),
    (9, NULL,  1800.00, 'online',        'prepayment', 'completed', 'TXN-20261125-12', '2026-11-25 08:05:00+02');

SELECT setval('hotel.booking_code_seq', 100);