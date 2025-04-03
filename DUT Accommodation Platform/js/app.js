const toggleIcon = document.getElementById("toggle-icon");
const logo = document.getElementById("logo");
const nav = document.getElementById("nav");
const navItems = document.querySelectorAll(".nav-links ul li");

// Initialize the sidebar state
let isCollapsed = false;

// Ensure all nav items have text wrapped in spans
navItems.forEach(item => {
    if (!item.querySelector('span.text')) {
        const textNodes = Array.from(item.childNodes)
            .filter(node => node.nodeType === Node.TEXT_NODE && node.textContent.trim() !== '');
        if (textNodes.length > 0) {
            const text = textNodes[0].textContent.trim();
            const icon = item.querySelector('i');
            item.innerHTML = `
                ${icon ? icon.outerHTML : ''}
                <span class="text">${text}</span>
            `;
        }
    }
});

toggleIcon.addEventListener("click", function () {
    // Toggle the collapsed state
    isCollapsed = !isCollapsed;

    // Toggle the icon
    this.classList.toggle("fa-bars");
    this.classList.toggle("fa-times");

    // Toggle the collapsed class
    nav.classList.toggle("collapsed", isCollapsed);

    // Smooth width transition
    nav.style.width = isCollapsed ? "60px" : "250px";

    // Handle logo visibility
    if (isCollapsed) {
        logo.style.opacity = "0";
        setTimeout(() => {
            logo.style.display = "none";
        }, 300);
    } else {
        logo.style.display = "block";
        setTimeout(() => {
            logo.style.opacity = "1";
        }, 10);
    }

    // Handle text labels
    document.querySelectorAll('.nav-links ul li .text').forEach(text => {
        if (isCollapsed) {
            text.style.opacity = "0";
            text.style.marginLeft = "0";
            setTimeout(() => {
                text.style.display = "none";
            }, 300);
        } else {
            text.style.display = "inline";
            setTimeout(() => {
                text.style.opacity = "1";
                text.style.marginLeft = "10px";
            }, 10);
        }
    });

    // Bounce animation
    navItems.forEach((item, index) => {
        item.style.transform = "scale(0.95)";
        setTimeout(() => {
            item.style.transform = "scale(1)";
        }, index * 50 + 100);
    });
});