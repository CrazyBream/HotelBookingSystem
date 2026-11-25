# StayFlow — Система бронювання готельних номерів

Курсовий проєкт: багатошаровий вебдодаток для пошуку, бронювання та адміністрування готельних номерів поверх реляційної бази даних **PostgreSQL 16** із кешуванням довідників у **Redis**.

## Стек технологій

- **СУБД:** PostgreSQL 16 (`btree_gist`, 11 таблиць у 3НФ, `EXCLUDE`-обмеження перетину дат, `VIEW`, SQL/PLpgSQL-функції, збережені процедури, `BEFORE`/`AFTER`-тригери, рольовий доступ)
- **Кешування:** Redis 7 (Docker, стратегія Cache-Aside + версіонована інвалідація через патерн Decorator)
- **Бекенд:** ASP.NET Core MVC (.NET 9), Dapper, Npgsql, BCrypt.Net-Next
- **Фронтенд:** Razor Views, Bootstrap 5, Vanilla JS (живий пошук вільних номерів і динамічний розрахунок вартості)

## Структура проєкту

```text
HotelBookingSystem/
├── database/                         # SQL-скрипти бази даних
│   ├── 01_schema.sql                 # Схема hotel, 11 таблиць, обмеження та EXCLUDE (GiST)
│   ├── 02_views.sql                  # Представлення каталогу, бронювань та аналітики
│   ├── 03_functions_triggers.sql     # Функції пошуку/розрахунку, процедури та тригери
│   ├── 04_roles.sql                  # Ролі (hotel_analyst, hotel_receptionist, hotel_admin)
│   ├── 05_seed.sql                   # Тестові дані з граничними випадками
│   ├── 06_queries.sql                # Аналітичні та операційні SQL-запити
│   ├── 07_tests.sql                  # Автоматизовані тести T01–T12b
│   └── 08_perf_and_indexes.sql       # Індекси та заміри EXPLAIN (ANALYZE, BUFFERS)
└── src/
    ├── HotelBooking.Domain/          # Сутності, моделі представлень, ієрархія винятків
    ├── HotelBooking.DAL/             # Dapper-репозиторії, BaseRepository, NpgsqlConnectionFactory
    ├── HotelBooking.BLL/             # DTO, бізнес-сервіси, CachedHotelService (Decorator)
    └── HotelBooking.Web/             # ASP.NET Core MVC, Middleware, Views, JS/CSS
