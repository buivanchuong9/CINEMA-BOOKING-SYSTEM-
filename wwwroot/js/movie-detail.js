/* ============================================
   MOVIE DETAIL PAGE - CineMax Cinema System
   Movie Info, Trailer, Showtime Selection
   ============================================ */

// ========== MOCK DATA: MOVIE DETAIL ==========
const movieDetail = {
    id: 4,
    title: "Avatar: The Way of Water",
    posterUrl: "https://images.unsplash.com/photo-1594908900066-3f47337549d8?w=400&h=600&fit=crop",
    backdropUrl: "https://images.unsplash.com/photo-1594908900066-3f47337549d8?w=1920&h=800&fit=crop",
    rating: 8.5,
    ageRating: "PG-13",
    genre: "Sci-Fi, Adventure, Fantasy",
    duration: "192 min",
    releaseDate: "2022-12-16",
    description: "Set more than a decade after the events of the first film, Avatar: The Way of Water begins to tell the story of the Sully family, the trouble that follows them, the lengths they go to keep each other safe, the battles they fight to stay alive, and the tragedies they endure.",
    director: "James Cameron",
    cast: "Sam Worthington, Zoe Saldana, Sigourney Weaver, Kate Winslet",
    trailerUrl: "https://www.youtube.com/watch?v=d9MyW72ELq0"
};

// ========== MOCK DATA: CINEMAS ==========
const cinemas = [
    { id: 1, name: "CineMax District 1", location: "123 Nguyen Hue, District 1" },
    { id: 2, name: "CineMax District 3", location: "456 Vo Van Tan, District 3" },
    { id: 3, name: "CineMax Thu Duc", location: "789 Xa Lo Ha Noi, Thu Duc" },
    { id: 4, name: "CineMax Binh Thanh", location: "321 Dien Bien Phu, Binh Thanh" }
];

// ========== MOCK DATA: SHOWTIMES ==========
const showtimes = {
    "2026-01-14": [
        { time: "09:00", type: "2D", available: 45, price: 80000, cinemaId: 1 },
        { time: "12:30", type: "3D IMAX", available: 32, price: 150000, cinemaId: 1 },
        { time: "15:00", type: "2D", available: 58, price: 80000, cinemaId: 1 },
        { time: "18:30", type: "3D IMAX", available: 28, price: 150000, cinemaId: 1 },
        { time: "21:00", type: "2D", available: 41, price: 80000, cinemaId: 1 },
        { time: "10:00", type: "2D", available: 52, price: 80000, cinemaId: 2 },
        { time: "13:00", type: "3D", available: 38, price: 120000, cinemaId: 2 },
        { time: "16:30", type: "2D", available: 44, price: 80000, cinemaId: 2 },
        { time: "20:00", type: "3D IMAX", available: 25, price: 150000, cinemaId: 2 }
    ],
    "2026-01-15": [
        { time: "10:30", type: "2D", available: 60, price: 80000, cinemaId: 1 },
        { time: "14:00", type: "3D IMAX", available: 40, price: 150000, cinemaId: 1 },
        { time: "17:30", type: "2D", available: 55, price: 80000, cinemaId: 1 },
        { time: "21:00", type: "3D", available: 35, price: 120000, cinemaId: 1 }
    ]
};

// ========== MOCK DATA: REVIEWS ==========
const reviews = [
    {
        id: 1,
        userName: "John Smith",
        rating: 5,
        date: "2026-01-10",
        comment: "Absolutely stunning! The visuals are breathtaking and the story is emotionally powerful. A must-see in IMAX 3D!",
        avatar: "https://i.pravatar.cc/150?img=12"
    },
    {
        id: 2,
        userName: "Emily Chen",
        rating: 4,
        date: "2026-01-08",
        comment: "A visual masterpiece. James Cameron delivers again. The underwater scenes are incredible.",
        avatar: "https://i.pravatar.cc/150?img=45"
    },
    {
        id: 3,
        userName: "Michael Brown",
        rating: 5,
        date: "2026-01-05",
        comment: "Worth the 13-year wait! The technology and storytelling blend perfectly. Emotional and epic.",
        avatar: "https://i.pravatar.cc/150?img=33"
    }
];

// ========== GLOBAL STATE ==========
let selectedDate = null;
let selectedCinemaId = null;
let flatpickrInstance = null;

// ========== INITIALIZE PAGE ==========
document.addEventListener('DOMContentLoaded', function() {
    loadMovieDetails();
    initializeDatePicker();
    populateCinemaSelect();
    renderReviews();
});

// ========== LOAD MOVIE DETAILS ==========
function loadMovieDetails() {
    // Set backdrop
    document.getElementById('movieBackdrop').style.backgroundImage = 
        `linear-gradient(to right, rgba(15, 23, 42, 0.95), rgba(15, 23, 42, 0.5)), url('${movieDetail.backdropUrl}')`;
    
    // Set poster
    document.getElementById('moviePoster').src = movieDetail.posterUrl;
    
    // Set basic info
    document.getElementById('movieRating').innerHTML = 
        `<i class="bi bi-star-fill me-1"></i> ${movieDetail.rating}`;
    document.getElementById('movieAgeRating').textContent = movieDetail.ageRating;
    document.getElementById('movieTitle').textContent = movieDetail.title;
    document.getElementById('movieDescription').textContent = movieDetail.description;
    document.getElementById('movieDirector').textContent = movieDetail.director;
    document.getElementById('movieCast').textContent = movieDetail.cast;
    
    // Set metadata
    const metaHtml = `
        <span class="badge bg-secondary fs-6">
            <i class="bi bi-tag-fill me-1"></i> ${movieDetail.genre}
        </span>
        <span class="badge bg-secondary fs-6">
            <i class="bi bi-clock-fill me-1"></i> ${movieDetail.duration}
        </span>
        <span class="badge bg-secondary fs-6">
            <i class="bi bi-calendar-fill me-1"></i> ${formatDate(movieDetail.releaseDate)}
        </span>
    `;
    document.getElementById('movieMeta').innerHTML = metaHtml;
}

// ========== INITIALIZE FLATPICKR DATE PICKER ==========
function initializeDatePicker() {
    const today = new Date();
    const maxDate = new Date();
    maxDate.setDate(today.getDate() + 7); // Next 7 days
    
    flatpickrInstance = flatpickr("#datePicker", {
        minDate: today,
        maxDate: maxDate,
        dateFormat: "Y-m-d",
        defaultDate: today,
        onChange: function(selectedDates, dateStr) {
            selectedDate = dateStr;
            loadShowtimes();
        },
        theme: "dark"
    });
    
    // Set initial date
    selectedDate = flatpickrInstance.formatDate(today, "Y-m-d");
}

// ========== POPULATE CINEMA SELECT ==========
function populateCinemaSelect() {
    const select = document.getElementById('cinemaSelect');
    
    cinemas.forEach(cinema => {
        const option = document.createElement('option');
        option.value = cinema.id;
        option.textContent = `${cinema.name} - ${cinema.location}`;
        select.appendChild(option);
    });
    
    // Set default cinema
    select.value = cinemas[0].id;
    selectedCinemaId = cinemas[0].id;
    
    select.addEventListener('change', function() {
        selectedCinemaId = parseInt(this.value);
        loadShowtimes();
    });
    
    // Load initial showtimes
    loadShowtimes();
}

// ========== LOAD SHOWTIMES ==========
function loadShowtimes() {
    const container = document.getElementById('showtimeGrid');
    
    if (!selectedDate || !selectedCinemaId) {
        container.innerHTML = '<p class="text-center text-muted">Please select a date and cinema</p>';
        return;
    }
    
    const availableShowtimes = (showtimes[selectedDate] || [])
        .filter(show => show.cinemaId === selectedCinemaId);
    
    if (availableShowtimes.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <i class="bi bi-calendar-x fs-1 text-muted mb-3 d-block"></i>
                <p class="text-muted">No showtimes available for this date and cinema.</p>
            </div>
        `;
        return;
    }
    
    let html = '<div class="row g-3">';
    
    availableShowtimes.forEach(show => {
        const isLowAvailability = show.available < 30;
        const availabilityClass = isLowAvailability ? 'text-warning' : 'text-success';
        
        html += `
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="showtime-card glass-card p-3 text-center hover-scale" 
                     onclick="selectShowtime('${show.time}', '${show.type}', ${show.price}, ${show.available})">
                    <div class="showtime-time mb-2">
                        <i class="bi bi-clock-fill text-cinema-red me-2"></i>
                        <span class="fs-4 fw-bold">${show.time}</span>
                    </div>
                    <div class="showtime-type mb-2">
                        <span class="badge ${show.type.includes('IMAX') ? 'badge-gold' : 'badge-cinema'}">
                            ${show.type}
                        </span>
                    </div>
                    <div class="showtime-price mb-2">
                        <span class="text-popcorn-gold fw-bold">${formatCurrency(show.price)}</span>
                    </div>
                    <div class="showtime-availability">
                        <small class="${availabilityClass}">
                            <i class="bi bi-person-fill"></i> ${show.available} seats left
                        </small>
                    </div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

// ========== SELECT SHOWTIME ==========
function selectShowtime(time, type, price, available) {
    if (available === 0) {
        Swal.fire({
            title: 'Sold Out',
            text: 'This showtime is fully booked. Please choose another time.',
            icon: 'error',
            confirmButtonColor: '#e11d48',
            background: '#1e293b',
            color: '#f8fafc'
        });
        return;
    }
    
    Swal.fire({
        title: 'Confirm Showtime',
        html: `
            <div class="text-start">
                <p><strong>Movie:</strong> ${movieDetail.title}</p>
                <p><strong>Cinema:</strong> ${cinemas.find(c => c.id === selectedCinemaId).name}</p>
                <p><strong>Date:</strong> ${formatDate(selectedDate)}</p>
                <p><strong>Time:</strong> ${time}</p>
                <p><strong>Format:</strong> ${type}</p>
                <p class="text-popcorn-gold fs-5"><strong>Price:</strong> ${formatCurrency(price)}/seat</p>
            </div>
        `,
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: 'Continue to Seat Selection',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#e11d48',
        cancelButtonColor: '#64748b',
        background: '#1e293b',
        color: '#f8fafc'
    }).then((result) => {
        if (result.isConfirmed) {
            // Redirect to seat selection page
            const bookingData = {
                movieId: movieDetail.id,
                movieTitle: movieDetail.title,
                cinemaId: selectedCinemaId,
                cinemaName: cinemas.find(c => c.id === selectedCinemaId).name,
                date: selectedDate,
                time: time,
                type: type,
                price: price
            };
            
            // Store in sessionStorage
            sessionStorage.setItem('bookingData', JSON.stringify(bookingData));
            
            // Redirect to seat selection
            window.location.href = '/Booking/SeatSelection';
        }
    });
}

// ========== RENDER REVIEWS ==========
function renderReviews() {
    const container = document.getElementById('reviewsContainer');
    
    reviews.forEach((review, index) => {
        const stars = '★'.repeat(review.rating) + '☆'.repeat(5 - review.rating);
        
        const reviewCard = `
            <div class="col-lg-4" data-aos="fade-up" data-aos-delay="${index * 100}">
                <div class="glass-card p-4 h-100">
                    <div class="d-flex align-items-center mb-3">
                        <img src="${review.avatar}" alt="${review.userName}" 
                             class="rounded-circle me-3" width="50" height="50">
                        <div>
                            <h6 class="mb-0">${review.userName}</h6>
                            <small class="text-muted">${formatDate(review.date)}</small>
                        </div>
                    </div>
                    <div class="text-popcorn-gold mb-2" style="font-size: 1.2rem;">
                        ${stars}
                    </div>
                    <p class="text-secondary mb-0">${review.comment}</p>
                </div>
            </div>
        `;
        container.innerHTML += reviewCard;
    });
}

// ========== PLAY MOVIE TRAILER ==========
function playMovieTrailer() {
    Swal.fire({
        html: `
            <div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
                <iframe 
                    style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;"
                    src="${movieDetail.trailerUrl.replace('watch?v=', 'embed/')}" 
                    frameborder="0" 
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
                    allowfullscreen>
                </iframe>
            </div>
        `,
        showConfirmButton: false,
        showCloseButton: true,
        width: '900px',
        background: '#1e293b',
        padding: '2rem'
    });
}

// ========== SHOW REVIEW FORM ==========
function showReviewForm() {
    Swal.fire({
        title: 'Write a Review',
        html: `
            <div class="text-start">
                <div class="mb-3">
                    <label class="form-label">Your Name</label>
                    <input type="text" id="reviewName" class="swal2-input" placeholder="Enter your name">
                </div>
                <div class="mb-3">
                    <label class="form-label">Rating</label>
                    <select id="reviewRating" class="swal2-select">
                        <option value="5">⭐⭐⭐⭐⭐ Excellent</option>
                        <option value="4">⭐⭐⭐⭐ Very Good</option>
                        <option value="3">⭐⭐⭐ Good</option>
                        <option value="2">⭐⭐ Fair</option>
                        <option value="1">⭐ Poor</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label">Your Review</label>
                    <textarea id="reviewComment" class="swal2-textarea" placeholder="Share your thoughts..."></textarea>
                </div>
            </div>
        `,
        confirmButtonText: 'Submit Review',
        showCancelButton: true,
        confirmButtonColor: '#e11d48',
        background: '#1e293b',
        color: '#f8fafc',
        preConfirm: () => {
            const name = document.getElementById('reviewName').value;
            const rating = document.getElementById('reviewRating').value;
            const comment = document.getElementById('reviewComment').value;
            
            if (!name || !comment) {
                Swal.showValidationMessage('Please fill in all fields');
            }
            
            return { name, rating, comment };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Thank You!',
                text: 'Your review has been submitted successfully.',
                icon: 'success',
                confirmButtonColor: '#e11d48',
                background: '#1e293b',
                color: '#f8fafc'
            });
        }
    });
}

// ========== SCROLL TO SHOWTIMES ==========
function scrollToShowtimes() {
    document.getElementById('showtimesSection').scrollIntoView({ behavior: 'smooth' });
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
