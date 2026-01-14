/* ============================================
   ADMIN GLOBAL SCRIPTS - CineMax Admin Portal
   Sidebar Toggle & Common Functions
   ============================================ */

document.addEventListener('DOMContentLoaded', function() {
    initSidebarToggle();
});

// ========== SIDEBAR TOGGLE ==========
function initSidebarToggle() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('adminSidebar');
    
    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('collapsed');
            
            // On mobile, use 'show' class instead
            if (window.innerWidth < 992) {
                sidebar.classList.toggle('show');
            }
        });
        
        // Close sidebar on mobile when clicking outside
        document.addEventListener('click', function(event) {
            if (window.innerWidth < 992) {
                const isClickInside = sidebar.contains(event.target) || 
                                     sidebarToggle.contains(event.target);
                
                if (!isClickInside && sidebar.classList.contains('show')) {
                    sidebar.classList.remove('show');
                }
            }
        });
    }
}

// ========== ACTIVE NAV ITEM ==========
// Set active nav item based on current URL
const currentPath = window.location.pathname;
const navItems = document.querySelectorAll('.nav-item');

navItems.forEach(item => {
    const href = item.getAttribute('href');
    if (href && currentPath.includes(href)) {
        item.classList.add('active');
    } else {
        item.classList.remove('active');
    }
});
