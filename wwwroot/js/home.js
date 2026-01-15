/* ============================================
   HOME PAGE - CineMax Cinema Booking System
   Utility functions for home page
   NOTE: Data is now loaded from database via HomeController
   ============================================ */

// ========== UTILITY: FORMAT DATE ==========
function formatDate(dateString) {
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return new Date(dateString).toLocaleDateString('vi-VN', options);
}
