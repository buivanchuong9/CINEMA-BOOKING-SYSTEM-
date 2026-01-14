/* ============================================
   ADMIN DASHBOARD - CineMax Admin Portal
   Charts & Analytics with Highcharts
   ============================================ */

// ========== MOCK DATA: DASHBOARD STATS ==========
const dashboardStats = {
    todayBookings: 247,
    todayRevenue: 19750000, // VND
    activeUsers: 1834,
    occupancyRate: 73.5
};

// ========== MOCK DATA: REVENUE (Last 7 Days) ==========
const revenueData = [
    { date: '2026-01-08', revenue: 15400000 },
    { date: '2026-01-09', revenue: 17200000 },
    { date: '2026-01-10', revenue: 21300000 },
    { date: '2026-01-11', revenue: 18900000 },
    { date: '2026-01-12', revenue: 16800000 },
    { date: '2026-01-13', revenue: 22500000 },
    { date: '2026-01-14', revenue: 19750000 }
];

// ========== MOCK DATA: TICKET SALES BY MOVIE ==========
const ticketSalesByMovie = [
    { movie: 'Avatar', tickets: 456 },
    { movie: 'Oppenheimer', tickets: 389 },
    { movie: 'Guardians Vol 3', tickets: 312 },
    { movie: 'John Wick 4', tickets: 298 },
    { movie: 'Spider-Man', tickets: 267 },
    { movie: 'The Flash', tickets: 234 },
    { movie: 'Mission Impossible', tickets: 198 },
    { movie: 'Barbie', tickets: 187 }
];

// ========== MOCK DATA: SEAT OCCUPANCY HEATMAP ==========
const occupancyHeatmapData = [
    [0, 0, 45], [0, 1, 52], [0, 2, 68], [0, 3, 72], [0, 4, 89], [0, 5, 95], [0, 6, 78],
    [1, 0, 38], [1, 1, 49], [1, 2, 71], [1, 3, 76], [1, 4, 92], [1, 5, 98], [1, 6, 82],
    [2, 0, 42], [2, 1, 55], [2, 2, 73], [2, 3, 79], [2, 4, 94], [2, 5, 99], [2, 6, 85],
    [3, 0, 51], [3, 1, 58], [3, 2, 75], [3, 3, 81], [3, 4, 96], [3, 5, 97], [3, 6, 88],
    [4, 0, 47], [4, 1, 54], [4, 2, 69], [4, 3, 74], [4, 4, 87], [4, 5, 93], [4, 6, 80],
    [5, 0, 36], [5, 1, 44], [5, 2, 59], [5, 3, 65], [5, 4, 78], [5, 5, 84], [5, 6, 72],
    [6, 0, 32], [6, 1, 41], [6, 2, 56], [6, 3, 61], [6, 4, 73], [6, 5, 79], [6, 6, 68]
];

// ========== MOCK DATA: TOP MOVIES ==========
const topMovies = [
    { title: 'Avatar: The Way of Water', bookings: 456, revenue: 36480000, trend: 'up' },
    { title: 'Oppenheimer', bookings: 389, revenue: 31120000, trend: 'up' },
    { title: 'Guardians of the Galaxy Vol. 3', bookings: 312, revenue: 24960000, trend: 'down' },
    { title: 'John Wick: Chapter 4', bookings: 298, revenue: 23840000, trend: 'up' },
    { title: 'Spider-Man: Across the Spider-Verse', bookings: 267, revenue: 21360000, trend: 'down' }
];

// ========== MOCK DATA: RECENT BOOKINGS ==========
const recentBookings = [
    {
        id: 'CM170526541234',
        customer: 'Nguyen Van A',
        movie: 'Avatar',
        cinema: 'CineMax District 1',
        datetime: '2026-01-14 20:30',
        seats: 'D-5, D-6',
        amount: 160000,
        status: 'confirmed'
    },
    {
        id: 'CM170526541235',
        customer: 'Tran Thi B',
        movie: 'Oppenheimer',
        cinema: 'CineMax District 3',
        datetime: '2026-01-14 18:00',
        seats: 'E-8, E-9, E-10',
        amount: 360000,
        status: 'confirmed'
    },
    {
        id: 'CM170526541236',
        customer: 'Le Van C',
        movie: 'Guardians Vol 3',
        cinema: 'CineMax Thu Duc',
        datetime: '2026-01-15 15:30',
        seats: 'F-4, F-5',
        amount: 240000,
        status: 'pending'
    },
    {
        id: 'CM170526541237',
        customer: 'Pham Thi D',
        movie: 'John Wick 4',
        cinema: 'CineMax Binh Thanh',
        datetime: '2026-01-15 19:00',
        seats: 'H-1',
        amount: 200000,
        status: 'confirmed'
    },
    {
        id: 'CM170526541238',
        customer: 'Hoang Van E',
        movie: 'Spider-Man',
        cinema: 'CineMax District 1',
        datetime: '2026-01-14 16:30',
        seats: 'C-3, C-4, C-5, C-6',
        amount: 320000,
        status: 'confirmed'
    }
];

// ========== INITIALIZE DASHBOARD ==========
document.addEventListener('DOMContentLoaded', function() {
    loadDashboardStats();
    initRevenueChart();
    initTicketSalesChart();
    initOccupancyHeatmap();
    loadTopMovies();
    loadRecentBookings();
});

// ========== LOAD DASHBOARD STATS ==========
function loadDashboardStats() {
    // Animate numbers
    animateValue('todayBookings', 0, dashboardStats.todayBookings, 1000);
    animateValue('todayRevenue', 0, dashboardStats.todayRevenue, 1000, true);
    animateValue('activeUsers', 0, dashboardStats.activeUsers, 1000);
    animateValue('occupancyRate', 0, dashboardStats.occupancyRate, 1000, false, '%');
}

// ========== REVENUE LINE CHART (Highcharts) ==========
function initRevenueChart() {
    const dates = revenueData.map(d => formatDateShort(d.date));
    const revenues = revenueData.map(d => d.revenue / 1000000); // Convert to millions

    Highcharts.chart('revenueChart', {
        chart: {
            type: 'line',
            backgroundColor: 'transparent',
            style: {
                fontFamily: 'Segoe UI, system-ui, -apple-system, sans-serif'
            }
        },
        title: {
            text: null
        },
        xAxis: {
            categories: dates,
            labels: {
                style: {
                    color: '#cbd5e1'
                }
            },
            lineColor: '#334155',
            tickColor: '#334155'
        },
        yAxis: {
            title: {
                text: 'Revenue (Million VND)',
                style: {
                    color: '#cbd5e1'
                }
            },
            labels: {
                style: {
                    color: '#cbd5e1'
                }
            },
            gridLineColor: '#334155'
        },
        tooltip: {
            backgroundColor: '#1e293b',
            borderColor: '#334155',
            style: {
                color: '#f8fafc'
            },
            formatter: function() {
                return '<b>' + this.x + '</b><br/>' +
                       'Revenue: ' + formatCurrency(this.y * 1000000);
            }
        },
        plotOptions: {
            line: {
                dataLabels: {
                    enabled: false
                },
                enableMouseTracking: true,
                marker: {
                    enabled: true,
                    radius: 5
                }
            }
        },
        series: [{
            name: 'Revenue',
            data: revenues,
            color: '#e11d48',
            lineWidth: 3,
            marker: {
                fillColor: '#e11d48',
                lineWidth: 2,
                lineColor: '#be123c'
            }
        }],
        credits: {
            enabled: false
        },
        legend: {
            enabled: false
        }
    });
}

// ========== TICKET SALES BAR CHART (Highcharts) ==========
function initTicketSalesChart() {
    const movies = ticketSalesByMovie.map(m => m.movie);
    const tickets = ticketSalesByMovie.map(m => m.tickets);

    Highcharts.chart('ticketSalesChart', {
        chart: {
            type: 'bar',
            backgroundColor: 'transparent'
        },
        title: {
            text: null
        },
        xAxis: {
            categories: movies,
            labels: {
                style: {
                    color: '#cbd5e1'
                }
            },
            lineColor: '#334155'
        },
        yAxis: {
            title: {
                text: 'Tickets Sold',
                style: {
                    color: '#cbd5e1'
                }
            },
            labels: {
                style: {
                    color: '#cbd5e1'
                }
            },
            gridLineColor: '#334155'
        },
        tooltip: {
            backgroundColor: '#1e293b',
            borderColor: '#334155',
            style: {
                color: '#f8fafc'
            },
            formatter: function() {
                return '<b>' + this.x + '</b><br/>' +
                       'Tickets: ' + this.y.toLocaleString();
            }
        },
        plotOptions: {
            bar: {
                dataLabels: {
                    enabled: true,
                    style: {
                        color: '#f8fafc',
                        textOutline: 'none'
                    }
                },
                colorByPoint: false
            }
        },
        series: [{
            name: 'Tickets',
            data: tickets,
            color: '#f59e0b'
        }],
        credits: {
            enabled: false
        },
        legend: {
            enabled: false
        }
    });
}

// ========== SEAT OCCUPANCY HEATMAP (Highcharts) ==========
function initOccupancyHeatmap() {
    Highcharts.chart('occupancyHeatmap', {
        chart: {
            type: 'heatmap',
            backgroundColor: 'transparent'
        },
        title: {
            text: null
        },
        xAxis: {
            categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            labels: {
                style: {
                    color: '#cbd5e1'
                }
            }
        },
        yAxis: {
            categories: ['6am-9am', '9am-12pm', '12pm-3pm', '3pm-6pm', '6pm-9pm', '9pm-12am', '12am-3am'],
            title: null,
            labels: {
                style: {
                    color: '#cbd5e1'
                }
            }
        },
        colorAxis: {
            min: 0,
            max: 100,
            stops: [
                [0, '#334155'],
                [0.5, '#f59e0b'],
                [1, '#e11d48']
            ]
        },
        tooltip: {
            backgroundColor: '#1e293b',
            borderColor: '#334155',
            style: {
                color: '#f8fafc'
            },
            formatter: function() {
                return '<b>' + this.series.yAxis.categories[this.point.y] + '</b><br/>' +
                       '<b>' + this.series.xAxis.categories[this.point.x] + '</b><br/>' +
                       'Occupancy: ' + this.point.value + '%';
            }
        },
        series: [{
            name: 'Occupancy',
            borderWidth: 2,
            borderColor: '#1e293b',
            data: occupancyHeatmapData,
            dataLabels: {
                enabled: true,
                color: '#f8fafc',
                style: {
                    textOutline: 'none',
                    fontWeight: 'bold'
                },
                formatter: function() {
                    return this.point.value + '%';
                }
            }
        }],
        credits: {
            enabled: false
        },
        legend: {
            enabled: false
        }
    });
}

// ========== LOAD TOP MOVIES ==========
function loadTopMovies() {
    const container = document.getElementById('topMoviesList');
    let html = '<div class="top-movies-list">';
    
    topMovies.forEach((movie, index) => {
        const trendIcon = movie.trend === 'up' ? 
            '<i class="bi bi-arrow-up-circle-fill text-success"></i>' : 
            '<i class="bi bi-arrow-down-circle-fill text-danger"></i>';
        
        html += `
            <div class="top-movie-item d-flex justify-content-between align-items-center mb-3 pb-3 ${index < topMovies.length - 1 ? 'border-bottom border-secondary' : ''}">
                <div class="flex-grow-1">
                    <div class="d-flex align-items-center gap-2 mb-1">
                        <span class="badge bg-cinema-red">${index + 1}</span>
                        <h6 class="mb-0">${movie.title}</h6>
                        ${trendIcon}
                    </div>
                    <small class="text-muted">${movie.bookings} bookings</small>
                </div>
                <div class="text-end">
                    <div class="text-popcorn-gold fw-bold">${formatCurrency(movie.revenue)}</div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

// ========== LOAD RECENT BOOKINGS ==========
function loadRecentBookings() {
    const tbody = document.getElementById('recentBookingsTable');
    let html = '';
    
    recentBookings.forEach(booking => {
        const statusBadge = booking.status === 'confirmed' ? 
            '<span class="badge bg-success">Confirmed</span>' : 
            '<span class="badge bg-warning text-dark">Pending</span>';
        
        html += `
            <tr>
                <td><code>${booking.id}</code></td>
                <td>${booking.customer}</td>
                <td>${booking.movie}</td>
                <td>${booking.cinema}</td>
                <td>${booking.datetime}</td>
                <td>${booking.seats}</td>
                <td class="text-popcorn-gold fw-bold">${formatCurrency(booking.amount)}</td>
                <td>${statusBadge}</td>
            </tr>
        `;
    });
    
    tbody.innerHTML = html;
}

// ========== UTILITY: ANIMATE VALUE ==========
function animateValue(id, start, end, duration, isCurrency = false, suffix = '') {
    const element = document.getElementById(id);
    const range = end - start;
    const increment = range / (duration / 16);
    let current = start;
    
    const timer = setInterval(() => {
        current += increment;
        if ((increment > 0 && current >= end) || (increment < 0 && current <= end)) {
            current = end;
            clearInterval(timer);
        }
        
        if (isCurrency) {
            element.textContent = formatCurrency(current);
        } else {
            element.textContent = Math.floor(current).toLocaleString() + suffix;
        }
    }, 16);
}

// ========== UTILITY: FORMAT CURRENCY ==========
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { 
        style: 'currency', 
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

// ========== UTILITY: FORMAT DATE SHORT ==========
function formatDateShort(dateString) {
    const date = new Date(dateString);
    const options = { month: 'short', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
}
