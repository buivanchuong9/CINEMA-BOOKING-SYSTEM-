/* ============================================
   SEAT SELECTION PAGE - CineMax Cinema System
   Interactive Seat Map with Panzoom Support
   ============================================ */

// ========== MOCK DATA: SEAT LAYOUT ==========
const seatLayout = {
    rows: ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'],
    seatsPerRow: 12,
    seatTypes: {
        'A': 'standard',
        'B': 'standard',
        'C': 'standard',
        'D': 'vip',
        'E': 'vip',
        'F': 'vip',
        'G': 'vip',
        'H': 'couple',
        'I': 'couple',
        'J': 'standard'
    },
    // Sold seats (row-seat format: "A-5")
    soldSeats: ['A-5', 'A-6', 'B-3', 'C-7', 'C-8', 'D-4', 'D-5', 'E-10', 'F-2', 'G-9', 'H-1', 'I-6'],
    prices: {
        standard: 80000,
        vip: 120000,
        couple: 200000
    }
};

// ========== GLOBAL STATE ==========
let selectedSeats = [];
let bookingData = {};
let panzoomInstance = null;

// ========== INITIALIZE PAGE ==========
document.addEventListener('DOMContentLoaded', function() {
    loadBookingInfo();
    generateSeatMap();
    initializePanzoom();
});

// ========== LOAD BOOKING INFO ==========
function loadBookingInfo() {
    const storedData = sessionStorage.getItem('bookingData');
    
    if (storedData) {
        bookingData = JSON.parse(storedData);
        
        document.getElementById('bookingMovie').textContent = bookingData.movieTitle;
        document.getElementById('bookingCinema').textContent = bookingData.cinemaName;
        document.getElementById('bookingDateTime').textContent = 
            `${formatDate(bookingData.date)} ${bookingData.time}`;
        document.getElementById('bookingFormat').textContent = bookingData.type;
    } else {
        // If no booking data, redirect back
        Swal.fire({
            title: 'No Booking Found',
            text: 'Please select a movie and showtime first.',
            icon: 'warning',
            confirmButtonColor: '#e11d48',
            background: '#1e293b',
            color: '#f8fafc'
        }).then(() => {
            window.location.href = '/';
        });
    }
}

// ========== GENERATE SEAT MAP ==========
function generateSeatMap() {
    const seatMapContainer = document.getElementById('seatMap');
    let html = '';
    
    seatLayout.rows.forEach(row => {
        const seatType = seatLayout.seatTypes[row];
        html += `<div class="seat-row" data-row="${row}">`;
        
        // Row label
        html += `<div class="seat-row-label">${row}</div>`;
        
        // Generate seats
        html += `<div class="seat-row-seats">`;
        for (let i = 1; i <= seatLayout.seatsPerRow; i++) {
            const seatId = `${row}-${i}`;
            const isSold = seatLayout.soldSeats.includes(seatId);
            const seatClass = getSeatClass(seatType, isSold);
            
            // For couple seats, merge every 2 seats
            if (seatType === 'couple' && i % 2 === 1) {
                const nextSeatId = `${row}-${i + 1}`;
                const isNextSold = seatLayout.soldSeats.includes(nextSeatId);
                
                if (isSold || isNextSold) {
                    html += `
                        <div class="seat couple-seat seat-sold" data-seat="${seatId}" data-type="${seatType}">
                            <i class="bi bi-lock-fill"></i>
                            <span class="seat-number">${i}-${i+1}</span>
                        </div>
                    `;
                    i++; // Skip next seat
                } else {
                    html += `
                        <div class="seat couple-seat ${seatClass}" data-seat="${seatId}" data-type="${seatType}" 
                             onclick="toggleSeat('${seatId}', '${seatType}', true)">
                            <i class="bi bi-heart-fill"></i>
                            <span class="seat-number">${i}-${i+1}</span>
                        </div>
                    `;
                    i++; // Skip next seat
                }
            } else if (seatType !== 'couple' || i % 2 === 1) {
                // Regular seats
                if (isSold) {
                    html += `
                        <div class="seat ${seatClass}" data-seat="${seatId}" data-type="${seatType}">
                            <i class="bi bi-x-circle-fill"></i>
                            <span class="seat-number">${i}</span>
                        </div>
                    `;
                } else {
                    html += `
                        <div class="seat ${seatClass}" data-seat="${seatId}" data-type="${seatType}" 
                             onclick="toggleSeat('${seatId}', '${seatType}', false)">
                            <span class="seat-number">${i}</span>
                        </div>
                    `;
                }
            }
            
            // Add aisle gap after seat 6
            if (i === 6) {
                html += `<div class="seat-aisle"></div>`;
            }
        }
        html += `</div>`;
        html += `</div>`;
    });
    
    seatMapContainer.innerHTML = html;
}

// ========== GET SEAT CLASS ==========
function getSeatClass(seatType, isSold) {
    if (isSold) return 'seat-sold';
    
    switch(seatType) {
        case 'vip':
            return 'seat-vip';
        case 'couple':
            return 'seat-couple';
        default:
            return 'seat-available';
    }
}

// ========== TOGGLE SEAT SELECTION ==========
function toggleSeat(seatId, seatType, isCouple) {
    // Check if seat is already sold
    if (seatLayout.soldSeats.includes(seatId)) {
        Swal.fire({
            title: 'Seat Unavailable',
            text: 'This seat has already been booked.',
            icon: 'error',
            confirmButtonColor: '#e11d48',
            background: '#1e293b',
            color: '#f8fafc',
            timer: 2000
        });
        return;
    }
    
    const seatElement = document.querySelector(`[data-seat="${seatId}"]`);
    const seatIndex = selectedSeats.findIndex(s => s.id === seatId);
    
    if (seatIndex > -1) {
        // Deselect seat
        selectedSeats.splice(seatIndex, 1);
        seatElement.classList.remove('seat-selected');
        
        if (seatType === 'vip') {
            seatElement.classList.add('seat-vip');
        } else if (seatType === 'couple') {
            seatElement.classList.add('seat-couple');
        } else {
            seatElement.classList.add('seat-available');
        }
    } else {
        // Select seat
        selectedSeats.push({
            id: seatId,
            type: seatType,
            price: seatLayout.prices[seatType],
            isCouple: isCouple
        });
        
        seatElement.classList.remove('seat-available', 'seat-vip', 'seat-couple');
        seatElement.classList.add('seat-selected');
    }
    
    updateBookingSummary();
}

// ========== UPDATE BOOKING SUMMARY ==========
function updateBookingSummary() {
    // Count seats by type
    const counts = {
        standard: 0,
        vip: 0,
        couple: 0
    };
    
    selectedSeats.forEach(seat => {
        counts[seat.type]++;
    });
    
    // Update counts
    document.getElementById('standardCount').textContent = counts.standard;
    document.getElementById('vipCount').textContent = counts.vip;
    document.getElementById('coupleCount').textContent = counts.couple;
    
    // Update prices
    document.getElementById('standardPrice').textContent = 
        formatCurrency(counts.standard * seatLayout.prices.standard);
    document.getElementById('vipPrice').textContent = 
        formatCurrency(counts.vip * seatLayout.prices.vip);
    document.getElementById('couplePrice').textContent = 
        formatCurrency(counts.couple * seatLayout.prices.couple);
    
    // Calculate total
    const total = selectedSeats.reduce((sum, seat) => sum + seat.price, 0);
    document.getElementById('totalPrice').textContent = formatCurrency(total);
    document.getElementById('totalPriceMobile').textContent = formatCurrency(total);
    document.getElementById('totalSeats').textContent = selectedSeats.length;
    
    // Update selected seats list
    updateSelectedSeatsList();
    
    // Enable/disable continue button
    const continueBtn = document.getElementById('continueBtn');
    const continueBtnMobile = document.getElementById('continueBtnMobile');
    
    if (selectedSeats.length > 0) {
        continueBtn.disabled = false;
        continueBtnMobile.disabled = false;
    } else {
        continueBtn.disabled = true;
        continueBtnMobile.disabled = true;
    }
}

// ========== UPDATE SELECTED SEATS LIST ==========
function updateSelectedSeatsList() {
    const container = document.getElementById('selectedSeatsList');
    
    if (selectedSeats.length === 0) {
        container.innerHTML = '<p class="text-muted small text-center">No seats selected</p>';
        return;
    }
    
    let html = '<div class="selected-seats-badges">';
    selectedSeats.forEach(seat => {
        const badgeClass = seat.type === 'vip' ? 'badge-gold' : 
                          seat.type === 'couple' ? 'badge-cinema' : 'bg-secondary';
        
        html += `
            <span class="badge ${badgeClass} me-1 mb-1">
                ${seat.id}
                <i class="bi bi-x-circle ms-1" onclick="toggleSeat('${seat.id}', '${seat.type}', ${seat.isCouple})" 
                   style="cursor: pointer;"></i>
            </span>
        `;
    });
    html += '</div>';
    
    container.innerHTML = html;
}

// ========== CLEAR SELECTION ==========
function clearSelection() {
    if (selectedSeats.length === 0) return;
    
    Swal.fire({
        title: 'Clear Selection?',
        text: 'Are you sure you want to clear all selected seats?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, clear all',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#e11d48',
        cancelButtonColor: '#64748b',
        background: '#1e293b',
        color: '#f8fafc'
    }).then((result) => {
        if (result.isConfirmed) {
            selectedSeats.forEach(seat => {
                const seatElement = document.querySelector(`[data-seat="${seat.id}"]`);
                seatElement.classList.remove('seat-selected');
                
                if (seat.type === 'vip') {
                    seatElement.classList.add('seat-vip');
                } else if (seat.type === 'couple') {
                    seatElement.classList.add('seat-couple');
                } else {
                    seatElement.classList.add('seat-available');
                }
            });
            
            selectedSeats = [];
            updateBookingSummary();
        }
    });
}

// ========== PROCEED TO PAYMENT ==========
function proceedToPayment() {
    if (selectedSeats.length === 0) {
        Swal.fire({
            title: 'No Seats Selected',
            text: 'Please select at least one seat to continue.',
            icon: 'warning',
            confirmButtonColor: '#e11d48',
            background: '#1e293b',
            color: '#f8fafc'
        });
        return;
    }
    
    const total = selectedSeats.reduce((sum, seat) => sum + seat.price, 0);
    
    // Save booking with seats
    const finalBooking = {
        ...bookingData,
        seats: selectedSeats,
        totalAmount: total,
        bookingId: generateBookingId()
    };
    
    sessionStorage.setItem('finalBooking', JSON.stringify(finalBooking));
    
    // Show confirmation
    Swal.fire({
        title: 'Confirm Booking',
        html: `
            <div class="text-start">
                <h6 class="mb-3">Booking Details</h6>
                <p><strong>Movie:</strong> ${bookingData.movieTitle}</p>
                <p><strong>Cinema:</strong> ${bookingData.cinemaName}</p>
                <p><strong>Date & Time:</strong> ${formatDate(bookingData.date)} ${bookingData.time}</p>
                <p><strong>Format:</strong> ${bookingData.type}</p>
                <p><strong>Seats:</strong> ${selectedSeats.map(s => s.id).join(', ')}</p>
                <hr>
                <h5 class="text-cinema-red"><strong>Total:</strong> ${formatCurrency(total)}</h5>
            </div>
        `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Proceed to Payment',
        cancelButtonText: 'Go Back',
        confirmButtonColor: '#e11d48',
        cancelButtonColor: '#64748b',
        background: '#1e293b',
        color: '#f8fafc'
    }).then((result) => {
        if (result.isConfirmed) {
            // Simulate payment process
            Swal.fire({
                title: 'Processing Payment...',
                html: 'Please wait while we process your booking.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                },
                background: '#1e293b',
                color: '#f8fafc'
            });
            
            // Simulate API call
            setTimeout(() => {
                Swal.fire({
                    title: 'Booking Successful!',
                    html: `
                        <div class="text-center">
                            <i class="bi bi-check-circle-fill text-success" style="font-size: 4rem;"></i>
                            <h5 class="mt-3 mb-3">Booking ID: ${finalBooking.bookingId}</h5>
                            <p>Your tickets have been sent to your email.</p>
                            <p class="text-muted small">Please arrive 15 minutes before showtime.</p>
                        </div>
                    `,
                    confirmButtonText: 'View Booking',
                    confirmButtonColor: '#e11d48',
                    background: '#1e293b',
                    color: '#f8fafc'
                }).then(() => {
                    window.location.href = '/';
                });
            }, 2000);
        }
    });
}

// ========== INITIALIZE PANZOOM (MOBILE) ==========
function initializePanzoom() {
    const seatMap = document.getElementById('seatMap');
    const isMobile = window.innerWidth < 992;
    
    if (isMobile) {
        panzoomInstance = Panzoom(seatMap, {
            maxScale: 3,
            minScale: 0.8,
            contain: 'outside',
            cursor: 'move'
        });
        
        // Enable pinch-to-zoom on touch devices
        const parent = seatMap.parentElement;
        parent.addEventListener('wheel', panzoomInstance.zoomWithWheel);
    }
}

// ========== ZOOM CONTROLS ==========
function zoomIn() {
    if (panzoomInstance) {
        panzoomInstance.zoomIn();
    }
}

function zoomOut() {
    if (panzoomInstance) {
        panzoomInstance.zoomOut();
    }
}

function resetZoom() {
    if (panzoomInstance) {
        panzoomInstance.reset();
    }
}

// ========== UTILITY: GENERATE BOOKING ID ==========
function generateBookingId() {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000);
    return `CM${timestamp}${random}`.substring(0, 12);
}

// ========== UTILITY: FORMAT CURRENCY ==========
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { 
        style: 'currency', 
        currency: 'VND' 
    }).format(amount);
}

// ========== UTILITY: FORMAT DATE ==========
function formatDate(dateString) {
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return new Date(dateString).toLocaleDateString('en-US', options);
}
