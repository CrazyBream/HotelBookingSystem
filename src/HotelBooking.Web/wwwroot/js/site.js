document.addEventListener("DOMContentLoaded", () => {
    initLiveRoomSearch();
    initLivePriceCalculator();
    initReceptionModals();
});

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

function formatMoney(value) {
    return Number(value).toLocaleString("uk-UA", {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
    });
}

function initLiveRoomSearch() {
    const form = document.getElementById("availabilitySearchForm");
    if (!form) return;

    const cityInput = document.getElementById("searchCity");
    const checkInInput = document.getElementById("searchCheckIn");
    const checkOutInput = document.getElementById("searchCheckOut");
    const guestsInput = document.getElementById("searchGuests");
    const container = document.getElementById("liveSearchContainer");
    const statusEl = document.getElementById("liveSearchStatus");
    const gridEl = document.getElementById("liveSearchGrid");

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const params = new URLSearchParams({
            city: cityInput.value,
            checkIn: checkInInput.value,
            checkOut: checkOutInput.value,
            guests: guestsInput.value
        });

        container.classList.remove("d-none");
        statusEl.textContent = "Пошук вільних номерів...";
        gridEl.innerHTML = "";

        try {
            const response = await fetch(`/api/rooms/search?${params.toString()}`, {
                headers: { "Accept": "application/json" }
            });
            const data = await response.json();

            if (!response.ok) {
                statusEl.textContent = "";
                gridEl.innerHTML = `<div class="col-12"><div class="alert alert-danger mb-0">${escapeHtml(data.error || "Помилка пошуку.")}</div></div>`;
                return;
            }

            statusEl.textContent = `Знайдено: ${data.length}`;

            if (data.length === 0) {
                gridEl.innerHTML = `<div class="col-12"><div class="alert alert-warning mb-0">На обрані дати вільних номерів не знайдено.</div></div>`;
                return;
            }

            gridEl.innerHTML = data.map((item) => {
                const stars = "★".repeat(item.starRating);
                const url = `/Hotels/Room/${item.roomId}?checkIn=${encodeURIComponent(checkInInput.value)}&checkOut=${encodeURIComponent(checkOutInput.value)}&guests=${encodeURIComponent(guestsInput.value)}`;
                return `
                    <div class="col-md-6 col-lg-4">
                        <div class="card h-100 shadow-sm room-card">
                            <div class="card-body d-flex flex-column">
                                <div class="d-flex justify-content-between align-items-start mb-2">
                                    <span class="badge bg-primary-subtle text-primary">${escapeHtml(item.city)}</span>
                                    <span class="text-warning fw-bold">${stars}</span>
                                </div>
                                <h5 class="card-title mb-1">${escapeHtml(item.hotelName)}</h5>
                                <p class="text-muted small mb-2">Номер ${escapeHtml(item.roomNumber)} · ${escapeHtml(item.categoryName)} (до ${item.maxGuests} гостей)</p>
                                <div class="mt-auto pt-3 border-top d-flex justify-content-between align-items-center">
                                    <div>
                                        <div class="fw-bold fs-5 text-primary">${formatMoney(item.totalStayPrice)} грн</div>
                                        <div class="small text-muted">${formatMoney(item.basePrice)} грн / ніч</div>
                                    </div>
                                    <a href="${url}" class="btn btn-outline-primary btn-sm">Обрати</a>
                                </div>
                            </div>
                        </div>
                    </div>`;
            }).join("");
        } catch {
            statusEl.textContent = "";
            gridEl.innerHTML = `<div class="col-12"><div class="alert alert-danger mb-0">Не вдалося зв’язатися із сервером.</div></div>`;
        }
    });
}

function initLivePriceCalculator() {
    const form = document.getElementById("bookingForm");
    if (!form) return;

    const roomId = form.dataset.roomId;
    const checkInInput = document.getElementById("bookingCheckIn");
    const checkOutInput = document.getElementById("bookingCheckOut");
    const metaEl = document.getElementById("priceCalculationMeta");
    const valueEl = document.getElementById("priceCalculationValue");

    const updatePrice = async () => {
        if (!checkInInput.value || !checkOutInput.value) return;

        if (checkOutInput.value <= checkInInput.value) {
            metaEl.textContent = "Дата виїзду має бути пізнішою за дату заїзду";
            valueEl.textContent = "—";
            return;
        }

        const params = new URLSearchParams({
            checkIn: checkInInput.value,
            checkOut: checkOutInput.value
        });

        try {
            const response = await fetch(`/api/rooms/${roomId}/price?${params.toString()}`, {
                headers: { "Accept": "application/json" }
            });
            const data = await response.json();

            if (!response.ok) {
                metaEl.textContent = data.error || "Некоректний період";
                valueEl.textContent = "—";
                return;
            }

            metaEl.textContent = `Вартість за ${data.nights} ноч.:`;
            valueEl.textContent = `${formatMoney(data.totalPrice)} грн`;
        } catch {
            metaEl.textContent = "Помилка розрахунку";
            valueEl.textContent = "—";
        }
    };

    checkInInput.addEventListener("change", updatePrice);
    checkOutInput.addEventListener("change", updatePrice);
}

function initReceptionModals() {
    const checkInModal = document.getElementById("checkInModal");
    if (checkInModal) {
        checkInModal.addEventListener("show.bs.modal", (event) => {
            const btn = event.relatedTarget;
            document.getElementById("checkInBookingId").value = btn.getAttribute("data-booking-id");
            document.getElementById("checkInBookingCode").textContent = btn.getAttribute("data-booking-code");
        });
    }

    const checkOutModal = document.getElementById("checkOutModal");
    if (checkOutModal) {
        checkOutModal.addEventListener("show.bs.modal", (event) => {
            const btn = event.relatedTarget;
            document.getElementById("checkOutBookingId").value = btn.getAttribute("data-booking-id");
            document.getElementById("checkOutBookingCode").textContent = btn.getAttribute("data-booking-code");
        });
    }
}