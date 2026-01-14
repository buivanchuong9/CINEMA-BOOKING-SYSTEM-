/* ============================================
   HOME PAGE - CineMax Cinema Booking System
   Hero Slider & Now Showing Movies
   ============================================ */

// ========== MOCK DATA: HERO MOVIES ==========
const heroMovies = [
    {
        id: 1,
        title: "Dune: Part Three",
        description: "The epic conclusion to the saga continues as Paul Atreides unites with the Fremen to seek revenge.",
        rating: 8.9,
        genre: "Sci-Fi, Adventure",
        duration: "165 min",
        releaseDate: "2026-03-15",
        backdropUrl: "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=1920&h=800&fit=crop",
        trailerUrl: "https://www.youtube.com/watch?v=U2Qp5pL3ovA"
    },
    {
        id: 2,
        title: "The Batman Returns",
        description: "Gotham's dark knight faces his greatest challenge yet as a new villain emerges from the shadows.",
        rating: 8.7,
        genre: "Action, Crime, Drama",
        duration: "178 min",
        releaseDate: "2026-06-20",
        backdropUrl: "https://images.unsplash.com/photo-1509347528160-9a9e33742cdb?w=1920&h=800&fit=crop",
        trailerUrl: "https://www.youtube.com/watch?v=mqqft2x_Aa4"
    },
    {
        id: 3,
        title: "Interstellar: Beyond",
        description: "A new crew embarks on a journey through the wormhole to save humanity's future.",
        rating: 9.1,
        genre: "Sci-Fi, Drama",
        duration: "189 min",
        releaseDate: "2026-11-10",
        backdropUrl: "https://images.unsplash.com/photo-1478720568477-152d9b164e26?w=1920&h=800&fit=crop",
        trailerUrl: "https://www.youtube.com/watch?v=zSWdZVtXT7E"
    }
];

// ========== MOCK DATA: NOW SHOWING MOVIES ==========
const nowShowingMovies = [
    {
        id: 4,
        title: "Avatar: The Way of Water",
        posterUrl: "https://images.unsplash.com/photo-1594908900066-3f47337549d8?w=400&h=600&fit=crop",
        rating: 8.5,
        genre: "Sci-Fi, Adventure",
        duration: "192 min",
        ageRating: "PG-13",
        showings: ["10:00", "13:30", "17:00", "20:30"]
    },
    {
        id: 5,
        title: "Oppenheimer",
        posterUrl: "https://images.unsplash.com/photo-1616530940355-351fabd9524b?w=400&h=600&fit=crop",
        rating: 8.8,
        genre: "Biography, Drama",
        duration: "180 min",
        ageRating: "R",
        showings: ["11:00", "14:30", "18:00", "21:30"]
    },
    {
        id: 6,
        title: "Guardians of the Galaxy Vol. 3",
        posterUrl: "https://images.unsplash.com/photo-1608889335941-32ac5f2041b9?w=400&h=600&fit=crop",
        rating: 8.2,
        genre: "Action, Comedy, Sci-Fi",
        duration: "150 min",
        ageRating: "PG-13",
        showings: ["09:30", "12:00", "15:30", "19:00"]
    },
    {
        id: 7,
        title: "John Wick: Chapter 4",
        posterUrl: "https://images.unsplash.com/photo-1509347528160-9a9e33742cdb?w=400&h=600&fit=crop",
        rating: 8.4,
        genre: "Action, Thriller",
        duration: "169 min",
        ageRating: "R",
        showings: ["12:30", "16:00", "19:30", "22:00"]
    },
    {
        id: 8,
        title: "Spider-Man: Across the Spider-Verse",
        posterUrl: "https://images.unsplash.com/photo-1635805737707-575885ab0820?w=400&h=600&fit=crop",
        rating: 8.9,
        genre: "Animation, Action, Adventure",
        duration: "140 min",
        ageRating: "PG",
        showings: ["10:30", "13:00", "16:30", "19:00"]
    },
    {
        id: 9,
        title: "The Flash",
        posterUrl: "https://images.unsplash.com/photo-1626814026160-2237a95fc5a0?w=400&h=600&fit=crop",
        rating: 7.8,
        genre: "Action, Adventure, Fantasy",
        duration: "144 min",
        ageRating: "PG-13",
        showings: ["11:30", "14:00", "17:30", "20:00"]
    },
    {
        id: 10,
        title: "Mission: Impossible - Dead Reckoning",
        posterUrl: "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=400&h=600&fit=crop",
        rating: 8.3,
        genre: "Action, Thriller",
        duration: "163 min",
        ageRating: "PG-13",
        showings: ["10:00", "13:30", "17:00", "20:30"]
    },
    {
        id: 11,
        title: "Barbie",
        posterUrl: "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=400&h=600&fit=crop",
        rating: 7.9,
        genre: "Comedy, Fantasy",
        duration: "114 min",
        ageRating: "PG-13",
        showings: ["09:00", "11:30", "14:00", "16:30", "19:00"]
    }
];

// ========== MOCK DATA: COMING SOON MOVIES ==========
const comingSoonMovies = [
    {
        id: 12,
        title: "Deadpool 3",
        posterUrl: "https://images.unsplash.com/photo-1609743522471-83c84ce23e32?w=400&h=600&fit=crop",
        releaseDate: "2026-07-26",
        genre: "Action, Comedy",
        ageRating: "R"
    },
    {
        id: 13,
        title: "Fantastic Four",
        posterUrl: "https://images.unsplash.com/photo-1478720568477-152d9b164e26?w=400&h=600&fit=crop",
        releaseDate: "2026-05-02",
        genre: "Action, Adventure, Sci-Fi",
        ageRating: "PG-13"
    },
    {
        id: 14,
        title: "Avatar 3",
        posterUrl: "https://images.unsplash.com/photo-1594908900066-3f47337549d8?w=400&h=600&fit=crop",
        releaseDate: "2026-12-18",
        genre: "Sci-Fi, Adventure",
        ageRating: "PG-13"
    },
    {
        id: 15,
        title: "The Marvels 2",
        posterUrl: "https://images.unsplash.com/photo-1626814026160-2237a95fc5a0?w=400&h=600&fit=crop",
        releaseDate: "2026-08-15",
        genre: "Action, Adventure, Fantasy",
        ageRating: "PG-13"
    }
];

// ========== INITIALIZE HERO SWIPER ==========
function initHeroSwiper() {
    const swiperWrapper = document.getElementById('heroSlides');
    
    // Generate hero slides from mock data
    heroMovies.forEach(movie => {
        const slide = `
            <div class="swiper-slide">
                <div class="hero-slide" style="background-image: linear-gradient(to right, rgba(15, 23, 42, 0.95), rgba(15, 23, 42, 0.3)), url('${movie.backdropUrl}');">
                    <div class="container h-100">
                        <div class="row h-100 align-items-center">
                            <div class="col-lg-6">
                                <div class="hero-content" data-aos="fade-right">
                                    <span class="badge badge-cinema mb-3">
                                        <i class="bi bi-star-fill me-1"></i> ${movie.rating}
                                    </span>
                                    <h1 class="display-3 fw-bold mb-3">${movie.title}</h1>
                                    <p class="lead text-secondary mb-3">${movie.description}</p>
                                    <div class="d-flex gap-3 mb-4 flex-wrap">
                                        <span class="badge bg-secondary">
                                            <i class="bi bi-tag-fill me-1"></i> ${movie.genre}
                                        </span>
                                        <span class="badge bg-secondary">
                                            <i class="bi bi-clock-fill me-1"></i> ${movie.duration}
                                        </span>
                                        <span class="badge bg-secondary">
                                            <i class="bi bi-calendar-fill me-1"></i> ${formatDate(movie.releaseDate)}
                                        </span>
                                    </div>
                                    <div class="d-flex gap-3">
                                        <a href="/Movies/Detail/${movie.id}" class="btn btn-cinema-red btn-lg rounded-pill px-5">
                                            <i class="bi bi-ticket-perforated-fill me-2"></i> Book Now
                                        </a>
                                        <button class="btn btn-outline-light btn-lg rounded-pill px-4" onclick="playTrailer('${movie.trailerUrl}')">
                                            <i class="bi bi-play-circle-fill me-2"></i> Watch Trailer
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        swiperWrapper.innerHTML += slide;
    });

    // Initialize Swiper
    new Swiper('.heroSwiper', {
        loop: true,
        autoplay: {
            delay: 5000,
            disableOnInteraction: false,
        },
        pagination: {
            el: '.swiper-pagination',
            clickable: true,
        },
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
        },
        effect: 'fade',
        fadeEffect: {
            crossFade: true
        },
        speed: 1000,
    });
}

// ========== RENDER NOW SHOWING MOVIES ==========
function renderNowShowingMovies() {
    const container = document.getElementById('nowShowingMovies');
    
    nowShowingMovies.forEach((movie, index) => {
        const card = `
            <div class="col-lg-3 col-md-4 col-sm-6" data-aos="fade-up" data-aos-delay="${index * 100}">
                <div class="movie-card">
                    <div class="movie-poster position-relative">
                        <img src="${movie.posterUrl}" alt="${movie.title}" class="img-fluid">
                        <div class="movie-overlay">
                            <div class="movie-info-quick">
                                <div class="mb-2">
                                    <span class="badge bg-warning text-dark">
                                        <i class="bi bi-star-fill me-1"></i> ${movie.rating}
                                    </span>
                                    <span class="badge bg-danger ms-2">${movie.ageRating}</span>
                                </div>
                                <p class="small mb-2">
                                    <i class="bi bi-tag-fill me-1"></i> ${movie.genre}
                                </p>
                                <p class="small mb-3">
                                    <i class="bi bi-clock-fill me-1"></i> ${movie.duration}
                                </p>
                                <a href="/Movies/Detail/${movie.id}" class="btn btn-cinema-red btn-sm w-100 rounded-pill">
                                    <i class="bi bi-ticket-perforated-fill me-1"></i> Book Ticket
                                </a>
                            </div>
                        </div>
                        <span class="movie-rating-badge">
                            <i class="bi bi-star-fill"></i> ${movie.rating}
                        </span>
                    </div>
                    <div class="movie-details">
                        <h5 class="movie-title mb-2">${movie.title}</h5>
                        <div class="showtime-quick">
                            ${movie.showings.slice(0, 3).map(time => 
                                `<span class="badge bg-secondary me-1 mb-1">${time}</span>`
                            ).join('')}
                        </div>
                    </div>
                </div>
            </div>
        `;
        container.innerHTML += card;
    });
}

// ========== RENDER COMING SOON MOVIES ==========
function renderComingSoonMovies() {
    const container = document.getElementById('comingSoonMovies');
    
    comingSoonMovies.forEach((movie, index) => {
        const card = `
            <div class="col-lg-3 col-md-4 col-sm-6" data-aos="zoom-in" data-aos-delay="${index * 100}">
                <div class="movie-card coming-soon-card">
                    <div class="movie-poster position-relative">
                        <img src="${movie.posterUrl}" alt="${movie.title}" class="img-fluid">
                        <div class="coming-soon-badge">
                            <i class="bi bi-calendar-event-fill"></i>
                            <span>Coming Soon</span>
                        </div>
                        <div class="movie-overlay">
                            <div class="movie-info-quick text-center">
                                <h6 class="mb-3">${movie.title}</h6>
                                <p class="small mb-2">
                                    <i class="bi bi-calendar3 me-1"></i> ${formatDate(movie.releaseDate)}
                                </p>
                                <p class="small mb-3">
                                    <i class="bi bi-tag-fill me-1"></i> ${movie.genre}
                                </p>
                                <button class="btn btn-outline-light btn-sm rounded-pill" onclick="notifyMe(${movie.id})">
                                    <i class="bi bi-bell-fill me-1"></i> Notify Me
                                </button>
                            </div>
                        </div>
                    </div>
                    <div class="movie-details">
                        <h5 class="movie-title mb-1">${movie.title}</h5>
                        <p class="text-muted small mb-0">
                            <i class="bi bi-calendar3 me-1"></i> ${formatDate(movie.releaseDate)}
                        </p>
                    </div>
                </div>
            </div>
        `;
        container.innerHTML += card;
    });
}

// ========== UTILITY: FORMAT DATE ==========
function formatDate(dateString) {
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return new Date(dateString).toLocaleDateString('en-US', options);
}

// ========== PLAY TRAILER ==========
function playTrailer(trailerUrl) {
    Swal.fire({
        html: `
            <div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden;">
                <iframe 
                    style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;"
                    src="${trailerUrl.replace('watch?v=', 'embed/')}" 
                    frameborder="0" 
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
                    allowfullscreen>
                </iframe>
            </div>
        `,
        showConfirmButton: false,
        showCloseButton: true,
        width: '800px',
        background: '#1e293b',
        customClass: {
            popup: 'trailer-modal'
        }
    });
}

// ========== NOTIFY ME FUNCTION ==========
function notifyMe(movieId) {
    Swal.fire({
        title: 'Get Notified!',
        html: `
            <input type="email" id="notifyEmail" class="swal2-input" placeholder="Enter your email">
        `,
        icon: 'info',
        confirmButtonText: 'Subscribe',
        confirmButtonColor: '#e11d48',
        showCancelButton: true,
        background: '#1e293b',
        color: '#f8fafc',
        preConfirm: () => {
            const email = document.getElementById('notifyEmail').value;
            if (!email) {
                Swal.showValidationMessage('Please enter your email');
            }
            return email;
        }
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Success!',
                text: 'You will be notified when this movie is released.',
                icon: 'success',
                confirmButtonColor: '#e11d48',
                background: '#1e293b',
                color: '#f8fafc'
            });
        }
    });
}

// ========== INITIALIZE ON PAGE LOAD ==========
document.addEventListener('DOMContentLoaded', function() {
    initHeroSwiper();
    renderNowShowingMovies();
    renderComingSoonMovies();
});
