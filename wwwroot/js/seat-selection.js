/* ============================================
   SEAT SELECTION PAGE - Real Data from Server
   ============================================ */

// ========== GLOBAL STATE ==========
let selectedSeats = [];
let showtimeId = 0;
let movieData = {};
let roomData = {};
let seatsData = [];
let seatStatusData = [];
let foodsData = [];
let panzoomInstance = null;

// Price lookup by seat type
const seatPrices = {};

// ========== INITIALIZE PAGE ==========
document.addEventListener('DOMContentLoaded', function () {
    loadDataFromServer();
    loadBookingInfo();
    generateSeatMap();
    loadFoodMenu();
    initializePanzoom();
    initializeSignalR();
});

// ========== LOAD DATA FROM SERVER ==========
function loadDataFromServer() {
    // Get data from ViewBag injected by server
    showtimeId = window.showtimeData?.id || 0;
    movieData = window.movieData || {};
    roomData = window.roomData || {};
    seatsData = window.seatsData || [];
    seatStatusData = window.seatStatusData || [];
    foodsData = window.foodsData || [];

    // Build price lookup from server data
    const basePrice = window.showtimeData?.basePrice || 80000;
    seatsData.forEach(seat => {
        if (seat.seatType && !seatPrices[seat.seatType.name]) {
            seatPrices[seat.seatType.name] = basePrice * (seat.seatType.surchargeRatio || 1.0);
        }
    });

    console.log('Loaded data:', { showtimeId, movieData, roomData, seatsCount: seatsData.length });
}

// ========== LOAD BOOKING INFO ==========
function loadBookingInfo() {
    if (!movieData.title) {
        Swal.fire({
            title: 'Lỗi',
            text: 'Không tìm thấy thông tin phim!',
            icon: 'error',
            confirmButtonColor: '#e11d48',
            background: '#1e293b',
            color: '#f8fafc'
        }).then(() => {
            window.location.href = '/Movies';
        });
        return;
    }

    document.getElementById('bookingMovie').textContent = movieData.title || '';
    document.getElementById('bookingCinema').textContent = roomData.cinemaName || '';
    document.getElementById('bookingDateTime').textContent = formatDateTime(movieData.startTime);
    document.getElementById('bookingFormat').textContent = roomData.name || '';
}

// ========== GENERATE SEAT MAP FROM DATABASE ==========
function generateSeatMap() {
    const seatMapContainer = document.getElementById('seatMap');
    if (!seatMapContainer) return;

    if (seatsData.length === 0) {
        seatMapContainer.innerHTML = '<p class="text-center text-muted">Không có ghế nào</p>';
        return;
    }

    // Group seats by row
    const seatsByRow = {};
    seatsData.forEach(seat => {
        const row = seat.rowNumber || seat.row || 'A';
        if (!seatsByRow[row]) {
            seatsByRow[row] = [];
        }
        seatsByRow[row].push(seat);
    });

    let html = '';
    Object.keys(seatsByRow).sort().forEach(row => {
        const rowSeats = seatsByRow[row].sort((a, b) => (a.seatNumber || a.number) - (b.seatNumber || b.number));

        html += `<div class="seat-row" data-row="${row}">`;
        html += `<div class="seat-row-label">${row}</div>`;
        html += `<div class="seat-row-seats">`;

        rowSeats.forEach(seat => {
            const seatRow = seat.rowNumber || seat.row || 'A';
            const seatNum = seat.seatNumber || seat.number || 1;
            const seatId = `${seatRow}-${seatNum}`;
            const seatTypeName = (seat.seatType?.name || 'Standard').toLowerCase();
            const status = getSeatStatusFromServer(seat.id);

            const seatClass = getSeatClass(seatTypeName, status);
            const isDisabled = status === 'sold' || status === 'held';

            html += `
                <div class="seat ${seatClass}" 
                     data-seat-id="${seat.id}"
                     data-seat="${seatId}" 
                     data-type="${seatTypeName}"
                     ${isDisabled ? 'data-disabled="true"' : ''}
                     onclick="${isDisabled ? '' : 'toggleSeat(this)'}">
                    ${isDisabled ? '<i class="bi bi-lock-fill"></i>' : ''}
                    <span class="seat-number">${seatNum}</span>
                </div>
            `;
        });

        html += `</div></div>`;
    });

    seatMapContainer.innerHTML = html;
}

// ========== GET SEAT STATUS FROM SERVER DATA ==========
function getSeatStatusFromServer(seatId) {
    const status = seatStatusData.find(s => s.seatId === seatId);
    if (!status) return 'available';

    const statusLower = (status.status || 'Available').toLowerCase();
    if (statusLower === 'sold') return 'sold';
    if (statusLower === 'held') return 'held';
    return 'available';
}

// ========== GET SEAT CSS CLASS ==========
function getSeatClass(seatType, status) {
    let classes = ['seat'];

    // Status class - Mutually Exclusive
    if (status === 'sold') {
        classes.push('seat-sold');
    } else if (status === 'held') {
        classes.push('seat-held');
    } else {
        classes.push('seat-available');
    }

    // Type class
    if (seatType === 'vip') classes.push('seat-vip');
    else if (seatType === 'couple') classes.push('seat-couple');
    // Default standard styling is applied by base .seat class or explicit standard class if needed

    return classes.join(' ');
}

// ========== TOGGLE SEAT SELECTION ==========
function toggleSeat(element) {
    if (element.dataset.disabled === 'true') return;

    const seatId = parseInt(element.dataset.seatId);
    const seatLabel = element.dataset.seat;
    const seatType = element.dataset.type;

    if (element.classList.contains('seat-selected')) {
        // Deselect
        element.classList.remove('seat-selected');
        selectedSeats = selectedSeats.filter(s => s.id !== seatId);
    } else {
        // Select
        element.classList.add('seat-selected');
        selectedSeats.push({
            id: seatId,
            label: seatLabel,
            type: seatType,
            price: seatPrices[seatType.charAt(0).toUpperCase() + seatType.slice(1)] || 80000
        });
    }

    updateSummary();
}

// ========== UPDATE BOOKING SUMMARY ==========
function updateSummary() {
    const standardSeats = selectedSeats.filter(s => s.type === 'standard');
    const vipSeats = selectedSeats.filter(s => s.type === 'vip');
    const coupleSeats = selectedSeats.filter(s => s.type === 'couple');

    const standardTotal = standardSeats.reduce((sum, s) => sum + s.price, 0);
    const vipTotal = vipSeats.reduce((sum, s) => sum + s.price, 0);
    const coupleTotal = coupleSeats.reduce((sum, s) => sum + s.price, 0);

    document.getElementById('standardCount').textContent = standardSeats.length;
    document.getElementById('standardPrice').textContent = formatCurrency(standardTotal);

    document.getElementById('vipCount').textContent = vipSeats.length;
    document.getElementById('vipPrice').textContent = formatCurrency(vipTotal);

    document.getElementById('coupleCount').textContent = coupleSeats.length;
    document.getElementById('couplePrice').textContent = formatCurrency(coupleTotal);

    // Food total
    const foodTotal = calculateFoodTotal();
    document.getElementById('foodTotal').textContent = formatCurrency(foodTotal);

    // Grand total
    const total = standardTotal + vipTotal + coupleTotal + foodTotal;
    document.getElementById('totalPrice').textContent = formatCurrency(total);
    document.getElementById('totalPriceMobile').textContent = formatCurrency(total);

    document.getElementById('totalSeats').textContent = selectedSeats.length;

    // Update selected seats list
    const listHtml = selectedSeats.length > 0
        ? selectedSeats.map(s => `
            <span class="badge bg-cinema-red me-1 mb-1">${s.label}</span>
          `).join('')
        : '<p class="text-muted small text-center">Chưa chọn ghế</p>';

    document.getElementById('selectedSeatsList').innerHTML = listHtml;

    // Enable/disable continue button
    const continueBtn = document.getElementById('continueBtn');
    const testPaymentBtn = document.getElementById('testPaymentBtn');
    const continueBtnMobile = document.getElementById('continueBtnMobile');

    if (selectedSeats.length > 0) {
        continueBtn.disabled = false;
        if (testPaymentBtn) testPaymentBtn.disabled = false;
        continueBtnMobile.disabled = false;
    } else {
        continueBtn.disabled = true;
        if (testPaymentBtn) testPaymentBtn.disabled = true;
        continueBtnMobile.disabled = true;
    }
}

// ========== CLEAR SELECTION ==========
function clearSelection() {
    selectedSeats = [];
    document.querySelectorAll('.seat-selected').forEach(el => {
        el.classList.remove('seat-selected');
    });

    // Clear food quantities
    document.querySelectorAll('.food-quantity').forEach(input => {
        input.value = 0;
    });

    updateSummary();
}

// ========== LOAD FOOD MENU ==========
function loadFoodMenu() {
    const foodContainer = document.getElementById('foodMenu');
    if (!foodContainer || foodsData.length === 0) return;

    const html = foodsData.map(food => `
        <div class="food-item">
            <img src="${food.imageUrl || '/images/placeholder-food.jpg'}" alt="${food.name}" class="food-image">
            <div class="food-info">
                <h6 class="food-name">${food.name}</h6>
                <p class="food-price">${formatCurrency(food.price)}</p>
            </div>
            <div class="food-quantity-control">
                <button class="btn btn-sm btn-outline-light" onclick="changeFoodQuantity(${food.id}, -1)">
                    <i class="bi bi-dash"></i>
                </button>
                <input type="number" class="food-quantity" id="food-${food.id}" 
                       value="0" min="0" max="10" data-price="${food.price}"
                       onchange="updateSummary()">
                <button class="btn btn-sm btn-outline-light" onclick="changeFoodQuantity(${food.id}, 1)">
                    <i class="bi bi-plus"></i>
                </button>
            </div>
        </div>
    `).join('');

    foodContainer.innerHTML = html;
}

// ========== CHANGE FOOD QUANTITY ==========
function changeFoodQuantity(foodId, delta) {
    const input = document.getElementById(`food-${foodId}`);
    if (!input) return;

    let newValue = parseInt(input.value) + delta;
    if (newValue < 0) newValue = 0;
    if (newValue > 10) newValue = 10;

    input.value = newValue;
    updateSummary();
}

// ========== CALCULATE FOOD TOTAL ==========
function calculateFoodTotal() {
    let total = 0;
    document.querySelectorAll('.food-quantity').forEach(input => {
        const quantity = parseInt(input.value) || 0;
        const price = parseFloat(input.dataset.price) || 0;
        total += quantity * price;
    });
    return total;
}

// ========== PROCEED TO PAYMENT ==========
async function proceedToPayment(useTestPayment = false) {
    if (selectedSeats.length === 0) {
        Swal.fire({
            title: 'Chưa chọn ghế',
            text: 'Vui lòng chọn ít nhất 1 ghế!',
            icon: 'warning',
            confirmButtonColor: '#e11d48',
            background: '#1e293b',
            color: '#f8fafc'
        });
        return;
    }

    // Collect selected foods
    const foods = [];
    document.querySelectorAll('.food-quantity').forEach(input => {
        const quantity = parseInt(input.value) || 0;
        if (quantity > 0) {
            const foodId = parseInt(input.id.replace('food-', ''));
            foods.push({ FoodId: foodId, Quantity: quantity });
        }
    });

    // Create booking form data - MVC THUẦN TÚY: Submit form POST trực tiếp
    const bookingData = {
        ShowtimeId: showtimeId,
        SeatIds: selectedSeats.map(s => s.id),
        Foods: foods,
        Notes: '',
        useTestPayment: useTestPayment // TEST MODE
    };

    // Submit form POST to /Booking/Create
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Booking/Create';

    // Add CSRF token
    const csrf = document.querySelector('input[name="__RequestVerificationToken"]');
    if (csrf) {
        form.appendChild(csrf.cloneNode(true));
    }

    // Add data as hidden inputs
    Object.keys(bookingData).forEach(key => {
        if (key === 'SeatIds' || key === 'Foods') {
            bookingData[key].forEach((item, index) => {
                if (key === 'SeatIds') {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = `${key}[${index}]`;
                    input.value = item;
                    form.appendChild(input);
                } else {
                    Object.keys(item).forEach(prop => {
                        const input = document.createElement('input');
                        input.type = 'hidden';
                        input.name = `${key}[${index}].${prop}`;
                        input.value = item[prop];
                        form.appendChild(input);
                    });
                }
            });
        } else {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = key;
            input.value = bookingData[key];
            form.appendChild(input);
        }
    });

    document.body.appendChild(form);
    form.submit();
}

// ========== INITIALIZE PANZOOM ==========
function initializePanzoom() {
    // Simple zoom for mobile - implement if needed
}

// ========== INITIALIZE SIGNALR ==========
function initializeSignalR() {
    // Connect to SeatHub - implement if needed
}

// ========== UTILITY FUNCTIONS ==========
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function formatDateTime(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString('vi-VN', {
        weekday: 'short',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function zoomIn() {
    // Implement zoom
}

function zoomOut() {
    // Implement zoom
}

function resetZoom() {
    // Implement zoom
}
