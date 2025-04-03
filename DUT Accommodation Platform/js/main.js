document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const mainContent = document.getElementById('mainContent');

    // Toggle sidebar
    sidebarToggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');

        // Store preference in localStorage
        const isCollapsed = sidebar.classList.contains('collapsed');
        localStorage.setItem('sidebarCollapsed', isCollapsed);
    });

    // Check for saved preference
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        sidebar.classList.add('collapsed');
    }

    // Auto-collapse on mobile
    function handleResize() {
        if (window.innerWidth < 992) {
            sidebar.classList.add('collapsed');
        } else {
            // Restore desktop state if not explicitly collapsed
            if (localStorage.getItem('sidebarCollapsed') !== 'true') {
                sidebar.classList.remove('collapsed');
            }
        }
    }

    // Initial check
    handleResize();

    // Listen for resize events
    window.addEventListener('resize', handleResize);
});