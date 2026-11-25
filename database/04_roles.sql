DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hotel_analyst') THEN
        CREATE ROLE hotel_analyst NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hotel_receptionist') THEN
        CREATE ROLE hotel_receptionist NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hotel_admin') THEN
        CREATE ROLE hotel_admin NOLOGIN;
    END IF;
END;
$$;

ALTER FUNCTION hotel.trg_fn_audit_booking_status()
    SECURITY DEFINER
    SET search_path = hotel, pg_catalog;

REVOKE ALL ON SCHEMA hotel FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA hotel FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA hotel FROM PUBLIC;
REVOKE ALL ON ALL ROUTINES IN SCHEMA hotel FROM PUBLIC;

REVOKE ALL ON SCHEMA hotel FROM hotel_analyst, hotel_receptionist, hotel_admin;
REVOKE ALL ON ALL TABLES IN SCHEMA hotel FROM hotel_analyst, hotel_receptionist, hotel_admin;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA hotel FROM hotel_analyst, hotel_receptionist, hotel_admin;
REVOKE ALL ON ALL ROUTINES IN SCHEMA hotel FROM hotel_analyst, hotel_receptionist, hotel_admin;

GRANT SELECT ON
    hotel.v_room_catalog,
    hotel.v_booking_summary,
    hotel.v_hotel_occupancy_stats,
    hotel.booking_audit
TO hotel_analyst;
GRANT EXECUTE ON FUNCTION
    hotel.fn_calculate_stay_price,
    hotel.fn_find_available_rooms
TO hotel_analyst;

GRANT USAGE ON SCHEMA hotel TO hotel_receptionist;

GRANT SELECT ON
    hotel.hotels,
    hotel.room_categories,
    hotel.rooms,
    hotel.amenities,
    hotel.room_amenities
TO hotel_receptionist;

GRANT SELECT (id, hotel_id, first_name, last_name, email, role_name, is_active, created_at)
    ON hotel.staff TO hotel_receptionist;

GRANT UPDATE (status) ON hotel.rooms TO hotel_receptionist;

GRANT SELECT, INSERT, UPDATE ON
    hotel.guests,
    hotel.bookings,
    hotel.stays,
    hotel.payments
TO hotel_receptionist;

GRANT SELECT ON
    hotel.v_room_catalog,
    hotel.v_booking_summary
TO hotel_receptionist;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA hotel TO hotel_receptionist;

GRANT EXECUTE ON FUNCTION
    hotel.fn_calculate_stay_price,
    hotel.fn_find_available_rooms
TO hotel_receptionist;

GRANT EXECUTE ON PROCEDURE
    hotel.sp_create_booking,
    hotel.sp_check_in_guest,
    hotel.sp_check_out_guest
TO hotel_receptionist;

GRANT ALL PRIVILEGES ON SCHEMA hotel TO hotel_admin;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA hotel TO hotel_admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA hotel TO hotel_admin;
GRANT ALL PRIVILEGES ON ALL ROUTINES IN SCHEMA hotel TO hotel_admin;