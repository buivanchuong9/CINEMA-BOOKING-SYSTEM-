/* ============================================
   MOVIE DETAIL PAGE - Real Data Only
   ============================================ */

// ========== GET DATA FROM SERVER ==========
const movieData = window.movieData || {};
const showtimesData = window.showtimesData || [];

// ========== PROCESS SHOWTIMES ==========
const showtimesByDate = {};
const cinemaMap = new Map();

showtimesData.forEach(st => {
    const date = st.startTime.split(' ')[0];
    if (!showtimesByDate[date]) {
        showtimesByDate[date] = [];
    }
    showtimesByDate[date].push(st);
    
    if (st.cinemaId && st.cinemaName) {
        cinemaMap.set(st.cinemaId, st.cinemaName);
    }
});

let selectedDate = null;
let selectedCinemaId = null;
let flatpickrInstance = null;

// ========== INITIALIZE ==========
document.addEventListener('DOMContentLoaded', function() {
    loadMovieInfo();
    initDatePicker();
    populateCinemas();
});

// ========== LOAD MOVIE INFO ==========
function loadMovieInfo() {
    if (!movieData || !movieData.title) return;
    
    // Backdrop
    const backdrop = document.getElementById('movieBackdrop');
    if (backdrop && movieData.posterUrl) {
        backdrop.style.backgroundImage = 
            `linear-gradient(to right, rgba(15, 23, 42, 0.95), rgba(15, 23, 42, 0.5)), url('${movieData.posterUrl}')`;
    }
    
    // Poster
    const poster = document.getElementById('moviePoster');
    if (poster && movieData.posterUrl) {
        poster.src = movieData.posterUrl;
        poster.alt = movieData.title;
    }
    
    // Title
    const title = document.getElementById('movieTitle');
    if (title) title.textContent = movieData.title;
    
    // Rating
    const rating = document.getElementById('movieRating');
    if (rating && movieData.rating) {
        rating.innerHTML = `<i class="bi bi-star-fill me-1"></i> ${movieData.rating}`;
    }
    
    // Age Rating
    const ageRating = document.getElementById('movieAgeRating');
    if (ageRating && movieData.ageRating) {
        ageRating.textContent = movieData.ageRating;
    }
    
    // Description
    const description = document.getElementById('movieDescription');
    if (description) description.textContent = movieData.description || '';
    
    // Director
    const director = document.getElementById('movieDirector');
    if (director) director.textContent = movieData.director || 'N/A';
    
    // Cast
    const cast = document.getElementById('movieCast');
    if (cast) cast.textContent = movieData.cast || 'N/A';
    
    // Meta
    const meta = document.getElementById('movieMeta');
    if (meta && movieData.durationMinutes && movieData.releaseDate) {
        meta.innerHTML = `
            <span class="badge bg-secondary fs-6">
                <i class="bi bi-clock-fill me-1"></i> ${movieData.durationMinutes} phút
            </span>
            <span class="badge bg-secondary fs-6">
                <i class="bi bi-calendar-fill me-1"></i> ${formatDate(movieData.releaseDate)}
            </span>
        `;
    }
}

// ========== DATE PICKER ==========
function initDatePicker() {
    const today = new Date();
    const maxDate = new Date();
    maxDate.setDate(today.getDate() + 30);
    
    const availableDates = Object.keys(showtimesByDate);
    
    flatpickrInstance = flatpickr("#datePicker", {
        minDate: today,
        maxDate: maxDate,
        dateFormat: "Y-m-d",
        defaultDate: availableDates.length > 0 ? availableDates[0] : today,
        enable: availableDates,
        onChange: function(selectedDates, dateStr) {
            selectedDate = dateStr;
            loadShowtimes();
        }
    });
    
    selectedDate = availableDates.length > 0 ? availableDates[0] : flatpickrInstance.formatDate(today, "Y-m-d");
    loadShowtimes();
}

// ========== POPULATE CINEMAS ==========
function populateCinemas() {
    const select = document.getElementById('cinemaSelect');
    if (!select) return;
    
    select.innerHTML = '<option value="">Tất cả rạp</option>';
    
    cinemaMap.forEach((name, id) => {
        const option = document.createElement('option');
        option.value = id;
        option.textContent = name;
        select.appendChild(option);
    });
    
    select.addEventListener('change', function() {
        selectedCinemaId = this.value ? parseInt(this.value) : null;
        loadShowtimes();
    });
}

// ========== LOAD SHOWTIMES ==========
function loadShowtimes() {
    const container = document.getElementById('showtimeGrid');
    if (!container) return;
    
    if (!selectedDate) {
        container.innerHTML = '<p class="text-center text-muted">Vui lòng chọn ngày</p>';
        return;
    }
    
    let showtimes = showtimesByDate[selectedDate] || [];
    
    if (selectedCinemaId) {
        showtimes = showtimes.filter(s => s.cinemaId === selectedCinemaId);
    }
    
    if (showtimes.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <i class="bi bi-calendar-x fs-1 text-muted mb-3 d-block"></i>
                <p class="text-muted">Không có suất chiếu</p>
            </div>
        `;
        return;
    }
    
    // Group by cinema
    const byCinema = {};
    showtimes.forEach(s => {
        const key = s.cinemaName || 'Unknown';
        if (!byCinema[key]) byCinema[key] = [];
        byCinema[key].push(s);
    });
    
    let html = '';
    
    Object.keys(byCinema).forEach(cinemaName => {
        html += `<div class="mb-4">
            <h5 class="text-popcorn-gold mb-3">
                <i class="bi bi-building me-2"></i>${cinemaName}
            </h5>
            <div class="row g-3">`;
        
        byCinema[cinemaName].forEach(s => {
            const time = s.startTime.split(' ')[1];
            html += `
                <div class="col-lg-2 col-md-3 col-sm-4 col-6">
                    <a href="/Booking/SelectSeats?showtimeId=${s.id}" 
                       class="btn btn-outline-light w-100 showtime-btn">
                        <div class="fw-bold fs-5 text-cinema-red">${time}</div>
                        <div class="small text-muted">${s.roomName || ''}</div>
                        <div class="small text-success">${formatCurrency(s.basePrice)}</div>
                    </a>
                </div>
            `;
        });
        
        html += '</div></div>';
    });
    
    container.innerHTML = html;
}

// ========== HELPERS ==========
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function formatDate(dateStr) {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat('vi-VN', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    }).format(date);
}

function scrollToShowtimes() {
    document.getElementById('showtimesSection')?.scrollIntoView({ 
        behavior: 'smooth' 
    });
}

function playMovieTrailer() {
    if (movieData.trailerUrl) {
        window.open(movieData.trailerUrl, '_blank');
    }
}
