/* ============================================
   CINEMAX - Global Site JavaScript
   Common Functions & Event Handlers
   ============================================ */

// ========== GLOBAL SEARCH FUNCTIONALITY ==========
const globalSearchInput = document.getElementById('globalSearch');

if (globalSearchInput) {
    globalSearchInput.addEventListener('input', debounce(function(e) {
        const query = e.target.value.trim();
        
        if (query.length < 2) {
            document.getElementById('searchResults').innerHTML = '';
            return;
        }
        
        performSearch(query);
    }, 300));
}

function performSearch(query) {
    // Mock search functionality - would call API in production
    const mockResults = [
        { type: 'movie', title: 'Avatar: The Way of Water', id: 4 },
        { type: 'movie', title: 'Oppenheimer', id: 5 },
        { type: 'cinema', title: 'CineMax District 1', id: 1 },
        { type: 'cinema', title: 'CineMax District 3', id: 2 }
    ].filter(item => item.title.toLowerCase().includes(query.toLowerCase()));
    
    displaySearchResults(mockResults);
}

function displaySearchResults(results) {
    const container = document.getElementById('searchResults');
    
    if (results.length === 0) {
        container.innerHTML = '<p class="text-muted text-center p-3">No results found</p>';
        return;
    }
    
    let html = '<div class="list-group">';
    
    results.forEach(result => {
        const icon = result.type === 'movie' ? 'bi-film' : 'bi-building';
        const url = result.type === 'movie' ? `/Movies/Detail/${result.id}` : `/Cinemas/${result.id}`;
        
        html += `
            <a href="${url}" class="list-group-item list-group-item-action bg-dark text-white border-secondary">
                <i class="bi ${icon} me-2"></i>
                ${result.title}
                <span class="badge bg-secondary float-end">${result.type}</span>
            </a>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

// ========== DEBOUNCE UTILITY ==========
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ========== NAVBAR SCROLL EFFECT ==========
let lastScroll = 0;
const navbar = document.querySelector('.cinema-navbar');

if (navbar) {
    window.addEventListener('scroll', () => {
        const currentScroll = window.pageYOffset;
        
        if (currentScroll > 100) {
            navbar.style.background = 'rgba(15, 23, 42, 0.98)';
            navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.5)';
        } else {
            navbar.style.background = 'rgba(15, 23, 42, 0.95)';
            navbar.style.boxShadow = '0 4px 16px rgba(0, 0, 0, 0.4)';
        }
        
        lastScroll = currentScroll;
    });
}

// ========== MOBILE MENU TOGGLE ==========
const navbarToggler = document.querySelector('.navbar-toggler');
const navbarCollapse = document.querySelector('.navbar-collapse');

if (navbarToggler && navbarCollapse) {
    navbarToggler.addEventListener('click', function() {
        navbarCollapse.classList.toggle('show');
    });
    
    // Close menu when clicking outside
    document.addEventListener('click', function(event) {
        const isClickInside = navbarToggler.contains(event.target) || 
                             navbarCollapse.contains(event.target);
        
        if (!isClickInside && navbarCollapse.classList.contains('show')) {
            navbarCollapse.classList.remove('show');
        }
    });
}

// ========== SMOOTH SCROLL FOR ANCHOR LINKS ==========
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        const href = this.getAttribute('href');
        if (href !== '#' && href !== '#!') {
            const target = document.querySelector(href);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        }
    });
});

// ========== LAZY LOAD IMAGES ==========
if ('IntersectionObserver' in window) {
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                if (img.dataset.src) {
                    img.src = img.dataset.src;
                    img.removeAttribute('data-src');
                    observer.unobserve(img);
                }
            }
        });
    });
    
    document.querySelectorAll('img[data-src]').forEach(img => {
        imageObserver.observe(img);
    });
}

// ========== TOAST NOTIFICATIONS ==========
function showToast(message, type = 'info') {
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        background: '#1e293b',
        color: '#f8fafc',
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer);
            toast.addEventListener('mouseleave', Swal.resumeTimer);
        }
    });
    
    Toast.fire({
        icon: type,
        title: message
    });
}

// ========== FORM VALIDATION HELPER ==========
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function validatePhone(phone) {
    const re = /^[0-9]{10,11}$/;
    return re.test(phone.replace(/[\s-]/g, ''));
}

// ========== LOCAL STORAGE HELPERS ==========
function setLocalStorage(key, value) {
    try {
        localStorage.setItem(key, JSON.stringify(value));
        return true;
    } catch (e) {
        console.error('Error saving to localStorage', e);
        return false;
    }
}

function getLocalStorage(key) {
    try {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    } catch (e) {
        console.error('Error reading from localStorage', e);
        return null;
    }
}

function removeLocalStorage(key) {
    try {
        localStorage.removeItem(key);
        return true;
    } catch (e) {
        console.error('Error removing from localStorage', e);
        return false;
    }
}

// ========== EXPORT COMMON FUNCTIONS ==========
window.CineMax = {
    showToast,
    validateEmail,
    validatePhone,
    setLocalStorage,
    getLocalStorage,
    removeLocalStorage,
    debounce
};

// ========== LOGIN/REGISTER MODALS ==========
function showLoginModal() {
    Swal.fire({
        title: '<i class="bi bi-person-circle me-2"></i>Login to CineMax',
        html: `
            <div class="text-start">
                <div class="mb-3">
                    <label class="form-label">Email</label>
                    <input type="email" id="loginEmail" class="swal2-input w-100" placeholder="your.email@example.com">
                </div>
                <div class="mb-3">
                    <label class="form-label">Password</label>
                    <input type="password" id="loginPassword" class="swal2-input w-100" placeholder="Enter your password">
                </div>
                <div class="form-check mb-3">
                    <input class="form-check-input" type="checkbox" id="rememberMe">
                    <label class="form-check-label" for="rememberMe">
                        Remember me
                    </label>
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'Login',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#e11d48',
        cancelButtonColor: '#64748b',
        background: '#1e293b',
        color: '#f8fafc',
        width: '500px',
        preConfirm: () => {
            const email = document.getElementById('loginEmail').value;
            const password = document.getElementById('loginPassword').value;
            
            if (!email || !password) {
                Swal.showValidationMessage('Please fill in all fields');
                return false;
            }
            
            if (!CineMax.validateEmail(email)) {
                Swal.showValidationMessage('Please enter a valid email');
                return false;
            }
            
            return { email, password };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            // Simulate login
            CineMax.showToast('Login successful! Welcome back!', 'success');
            // In production: send to backend API
        }
    });
}

function showRegisterModal() {
    Swal.fire({
        title: '<i class="bi bi-person-plus me-2"></i>Register to CineMax',
        html: `
            <div class="text-start">
                <div class="mb-3">
                    <label class="form-label">Full Name</label>
                    <input type="text" id="regName" class="swal2-input w-100" placeholder="Nguyen Van A">
                </div>
                <div class="mb-3">
                    <label class="form-label">Email</label>
                    <input type="email" id="regEmail" class="swal2-input w-100" placeholder="your.email@example.com">
                </div>
                <div class="mb-3">
                    <label class="form-label">Phone</label>
                    <input type="tel" id="regPhone" class="swal2-input w-100" placeholder="0901234567">
                </div>
                <div class="mb-3">
                    <label class="form-label">Password</label>
                    <input type="password" id="regPassword" class="swal2-input w-100" placeholder="Min 6 characters">
                </div>
                <div class="mb-3">
                    <label class="form-label">Confirm Password</label>
                    <input type="password" id="regConfirmPassword" class="swal2-input w-100" placeholder="Re-enter password">
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'Register',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#e11d48',
        cancelButtonColor: '#64748b',
        background: '#1e293b',
        color: '#f8fafc',
        width: '500px',
        preConfirm: () => {
            const name = document.getElementById('regName').value;
            const email = document.getElementById('regEmail').value;
            const phone = document.getElementById('regPhone').value;
            const password = document.getElementById('regPassword').value;
            const confirmPassword = document.getElementById('regConfirmPassword').value;
            
            if (!name || !email || !phone || !password || !confirmPassword) {
                Swal.showValidationMessage('Please fill in all fields');
                return false;
            }
            
            if (!CineMax.validateEmail(email)) {
                Swal.showValidationMessage('Please enter a valid email');
                return false;
            }
            
            if (!CineMax.validatePhone(phone)) {
                Swal.showValidationMessage('Please enter a valid phone number');
                return false;
            }
            
            if (password.length < 6) {
                Swal.showValidationMessage('Password must be at least 6 characters');
                return false;
            }
            
            if (password !== confirmPassword) {
                Swal.showValidationMessage('Passwords do not match');
                return false;
            }
            
            return { name, email, phone, password };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            // Simulate registration
            CineMax.showToast('Registration successful! Welcome to CineMax!', 'success');
            // In production: send to backend API
        }
    });
}

