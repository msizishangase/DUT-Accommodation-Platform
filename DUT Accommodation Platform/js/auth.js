document.addEventListener('DOMContentLoaded', function () {
    // Initialize all components (call each init function only once)
    initFloatingLabels();
    initPasswordStrengthMeter();
    initPasswordToggle();  // This is the only call to initPasswordToggle
    initFormValidation();
    initEmailValidation();
    initPasswordRequirements();

    // Remove any other calls to initPasswordToggle() in your code
});



function initFloatingLabels() {
    const floatLabels = document.querySelectorAll('.floating-label');

    floatLabels.forEach(group => {
        const input = group.querySelector('input');
        const label = group.querySelector('label');

        // Check if input has value on load
        if (input.value) {
            label.classList.add('floating');
        }

        input.addEventListener('input', () => {
            if (input.value) {
                label.classList.add('floating');
            } else {
                label.classList.remove('floating');
            }
        });
    });
}

function initPasswordStrengthMeter() {
    const passwordInput = document.getElementById('Password');
    if (!passwordInput) return;

    const strengthIndicator = document.getElementById('strengthIndicator');
    const strengthText = document.getElementById('strengthText');

    passwordInput.addEventListener('input', function (e) {
        const password = e.target.value;
        const strength = calculatePasswordStrength(password);

        updateStrengthIndicator(strength, strengthIndicator, strengthText);
    });
}

function calculatePasswordStrength(password) {
    let strength = 0;

    // Length check
    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;

    // Character diversity
    if (/[A-Z]/.test(password)) strength++; // Uppercase
    if (/[0-9]/.test(password)) strength++; // Numbers
    if (/[^A-Za-z0-9]/.test(password)) strength++; // Special chars

    return Math.min(strength, 5); // Cap at 5
}

function updateStrengthIndicator(strength, indicator, textElement) {
    const percentage = (strength / 5) * 100;
    indicator.style.width = `${percentage}%`;

    // Update colors and text
    if (strength <= 1) {
        indicator.style.backgroundColor = 'var(--error)';
        textElement.textContent = 'Very Weak';
        textElement.style.color = 'var(--error)';
    } else if (strength <= 2) {
        indicator.style.backgroundColor = 'var(--warning)';
        textElement.textContent = 'Weak';
        textElement.style.color = 'var(--warning)';
    } else if (strength <= 3) {
        indicator.style.backgroundColor = '#F59E0B';
        textElement.textContent = 'Medium';
        textElement.style.color = '#F59E0B';
    } else if (strength <= 4) {
        indicator.style.backgroundColor = 'var(--success)';
        textElement.textContent = 'Strong';
        textElement.style.color = 'var(--success)';
    } else {
        indicator.style.backgroundColor = '#10B981';
        textElement.textContent = 'Very Strong';
        textElement.style.color = '#10B981';
    }
}

function initFormValidation() {
    const form = document.getElementById('registrationForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        let isValid = true;
        const inputs = form.querySelectorAll('input[required]');

        inputs.forEach(input => {
            if (!input.checkValidity()) {
                input.classList.add('invalid');
                isValid = false;
            }
        });

        // Check password match
        const password = document.getElementById('Password');
        const confirmPassword = document.getElementById('ConfirmPassword');

        if (password.value !== confirmPassword.value) {
            confirmPassword.classList.add('invalid');
            showError(confirmPassword, 'Passwords do not match');
            isValid = false;
        }

        if (!isValid) {
            e.preventDefault();
            // Focus on first invalid input
            const firstInvalid = form.querySelector('.invalid');
            if (firstInvalid) {
                firstInvalid.focus();
                firstInvalid.classList.add('shake');
                setTimeout(() => firstInvalid.classList.remove('shake'), 500);
            }
        }
    });

    // Clear invalid state on input
    form.querySelectorAll('input').forEach(input => {
        input.addEventListener('input', function () {
            this.classList.remove('invalid');
            const errorElement = this.nextElementSibling;
            if (errorElement && errorElement.classList.contains('error-message')) {
                errorElement.remove();
            }
        });
    });
}

function initEmailValidation() {
    const emailInput = document.getElementById('Email');
    if (!emailInput) return;

    emailInput.addEventListener('blur', function () {
        const emailRegex = /^\d{8}@dut4life\.ac\.za$/;
        if (!emailRegex.test(this.value)) {
            showError(this, 'Please enter a valid DUT student email (22202887@dut4life.ac.za)');
        }
    });
}

function initPasswordRequirements() {
    const passwordInput = document.getElementById('Password');
    if (!passwordInput) return;

    const requirements = {
        length: { regex: /^.{8,}$/, element: document.querySelector('[data-requirement="length"]') },
        uppercase: { regex: /[A-Z]/, element: document.querySelector('[data-requirement="uppercase"]') },
        number: { regex: /[0-9]/, element: document.querySelector('[data-requirement="number"]') },
        special: { regex: /[^A-Za-z0-9]/, element: document.querySelector('[data-requirement="special"]') }
    };

    passwordInput.addEventListener('input', function () {
        for (const [key, req] of Object.entries(requirements)) {
            if (req.regex.test(this.value)) {
                req.element.classList.add('valid');
            } else {
                req.element.classList.remove('valid');
            }
        }
    });
}

function showError(input, message) {
    // Remove existing error message if any
    let errorElement = input.nextElementSibling;
    while (errorElement && !errorElement.classList.contains('error-message')) {
        errorElement = errorElement.nextElementSibling;
    }

    if (!errorElement) {
        errorElement = document.createElement('div');
        errorElement.className = 'error-message';
        input.parentNode.insertBefore(errorElement, input.nextElementSibling);
    }

    errorElement.textContent = message;
    errorElement.style.color = 'var(--error)';
    errorElement.style.fontSize = '0.75rem';
    errorElement.style.marginTop = '0.25rem';
    input.classList.add('invalid');
}

document.addEventListener('DOMContentLoaded', function () {
    // Get form elements
    const form = document.getElementById('registrationForm');
    const registerButton = document.getElementById('registerButton');
    const inputs = {
        fullName: document.getElementById('FullName'),
        email: document.getElementById('Email'),
        password: document.getElementById('Password'),
        confirmPassword: document.getElementById('ConfirmPassword')
    };
    const errorElements = {
        fullName: document.getElementById('fullNameError'),
        email: document.getElementById('emailError'),
        password: document.getElementById('passwordError'),
        confirmPassword: document.getElementById('confirmPasswordError')
    };

    // Initialize all event listeners
    initValidation();

    function initValidation() {
        // Add event listeners to all inputs
        inputs.fullName.addEventListener('input', validateFullName);
        inputs.email.addEventListener('input', validateEmail);
        inputs.password.addEventListener('input', validatePassword);
        inputs.confirmPassword.addEventListener('input', validateConfirmPassword);

        // Also validate on blur
        inputs.fullName.addEventListener('blur', validateFullName);
        inputs.email.addEventListener('blur', validateEmail);
        inputs.password.addEventListener('blur', validatePassword);
        inputs.confirmPassword.addEventListener('blur', validateConfirmPassword);

        // Validate entire form on any input change
        form.addEventListener('input', validateForm);
    }

    // Validation functions
    function validateFullName() {
        const value = inputs.fullName.value.trim();
        const regex = /^[a-zA-Z\s'-]+$/; // Only letters, spaces, hyphens, and apostrophes

        if (!value) {
            showError(inputs.fullName, errorElements.fullName, 'Full name is required');
            return false;
        } else if (/\d/.test(value)) {
            showError(inputs.fullName, errorElements.fullName, 'Numbers are not allowed in name');
            return false;
        } else if (!regex.test(value)) {
            showError(inputs.fullName, errorElements.fullName, 'Invalid characters in name');
            return false;
        } else {
            clearError(inputs.fullName, errorElements.fullName);
            return true;
        }
    }

    function validateEmail() {
        const value = inputs.email.value.trim();
        const regex = /^\d{8}@dut4life\.ac\.za$/;

        if (!value) {
            showError(inputs.email, errorElements.email, 'Email is required');
            return false;
        } else if (!regex.test(value)) {
            showError(inputs.email, errorElements.email, 'Please enter a valid DUT student email (22202887@dut4life.ac.za)');
            return false;
        } else {
            clearError(inputs.email, errorElements.email);
            return true;
        }
    }

    function validatePassword() {
        const value = inputs.password.value;
        const requirements = {
            length: value.length >= 8,
            uppercase: /[A-Z]/.test(value),
            number: /[0-9]/.test(value),
            special: /[^A-Za-z0-9]/.test(value)
        };

        // Update password requirements UI
        Object.entries(requirements).forEach(([key, met]) => {
            const element = document.querySelector(`[data-requirement="${key}"]`);
            if (element) element.classList.toggle('valid', met);
        });

        if (!value) {
            showError(inputs.password, errorElements.password, 'Password is required');
            return false;
        } else if (value.length < 8) {
            showError(inputs.password, errorElements.password, 'Password must be at least 8 characters');
            return false;
        } else {
            clearError(inputs.password, errorElements.password);
            return true;
        }
    }

    function validateConfirmPassword() {
        const password = inputs.password.value;
        const confirmPassword = inputs.confirmPassword.value;

        if (!confirmPassword) {
            showError(inputs.confirmPassword, errorElements.confirmPassword, 'Please confirm your password');
            return false;
        } else if (password !== confirmPassword) {
            showError(inputs.confirmPassword, errorElements.confirmPassword, 'Passwords do not match');
            return false;
        } else {
            clearError(inputs.confirmPassword, errorElements.confirmPassword);
            return true;
        }
    }

    // Form validation
    function validateForm() {
        const isFullNameValid = validateFullName();
        const isEmailValid = validateEmail();
        const isPasswordValid = validatePassword();
        const isConfirmPasswordValid = validateConfirmPassword();

        // Enable/disable register button based on validation
        registerButton.disabled = !(isFullNameValid && isEmailValid && isPasswordValid && isConfirmPasswordValid);

        return isFullNameValid && isEmailValid && isPasswordValid && isConfirmPasswordValid;
    }

    // Form submission
    form.addEventListener('submit', function (e) {
        e.preventDefault();

        if (validateForm()) {
            // Form is valid, proceed with submission
            form.submit();
        }
    });

    // Helper functions
    function showError(input, errorElement, message) {
        input.parentElement.classList.add('invalid');
        errorElement.textContent = message;
        errorElement.style.display = 'block';
    }

    function clearError(input, errorElement) {
        input.parentElement.classList.remove('invalid');
        errorElement.style.display = 'none';
        errorElement.textContent = '';
    }
});

function setupFloatingLabels() {
    document.querySelectorAll('.input-container').forEach(container => {
        const input = container.querySelector('input');
        const label = container.querySelector('label');
        const icon = container.querySelector('.input-icon');

        // Initialize state
        if (input.value) {
            container.classList.add('has-value');
        }

        // Handle focus events
        input.addEventListener('focus', () => {
            container.classList.add('focused');
        });

        input.addEventListener('blur', () => {
            container.classList.remove('focused');
            if (input.value) {
                container.classList.add('has-value');
            } else {
                container.classList.remove('has-value');
            }
        });

        // Handle input changes
        input.addEventListener('input', () => {
            if (input.value) {
                container.classList.add('has-value');
            } else {
                container.classList.remove('has-value');
            }
        });
    });
}

document.addEventListener('DOMContentLoaded', function () {
    setupFloatingLabels();
    // Rest of your initialization code...
});

function initPasswordToggle() {
    document.querySelectorAll('.password-toggle').forEach(button => {
        button.addEventListener('click', function () {
            const input = this.closest('.input-container').querySelector('input[type="password"], input[type="text"]');
            const icon = this.querySelector('i');

            if (input) {
                if (input.type === 'password') {
                    input.type = 'text'; // Reveal password
                    icon.classList.replace('fa-eye', 'fa-eye-slash'); // Change icon to slashed eye
                    this.setAttribute('aria-label', 'Hide password');
                } else {
                    input.type = 'password'; // Hide password
                    icon.classList.replace('fa-eye-slash', 'fa-eye'); // Change icon back
                    this.setAttribute('aria-label', 'Show password');
                }
            }
        });
    });
}

// Ensure the function is called only once
document.addEventListener('DOMContentLoaded', function () {
    initPasswordToggle();
});

function validatePhoneNumber() {
    const value = inputs.phoneNumber.value.trim();
    const regex = /^[+]?[\d\s-]{10,}$/; // Allows international numbers

    if (!value) {
        showError(inputs.phoneNumber, errorElements.phone, 'Phone number is required');
        return false;
    } else if (!regex.test(value)) {
        showError(inputs.phoneNumber, errorElements.phone, 'Please enter a valid phone number');
        return false;
    } else {
        clearError(inputs.phoneNumber, errorElements.phone);
        return true;
    }
}

function validateCompanyName() {
    const value = inputs.companyName.value.trim();
    // Company name is optional, no validation needed except length which is handled by server
    clearError(inputs.companyName, errorElements.company);
    return true;
}

function validateTerms() {
    const isChecked = inputs.terms.checked;

    if (!isChecked) {
        showError(inputs.terms, errorElements.terms, 'You must accept the terms and conditions');
        return false;
    } else {
        clearError(inputs.terms, errorElements.terms);
        return true;
    }
}

// Update the validateForm function to include these new validations
function validateForm() {
    const isFullNameValid = validateFullName();
    const isEmailValid = validateEmail();
    const isPhoneValid = validatePhoneNumber();
    const isPasswordValid = validatePassword();
    const isConfirmPasswordValid = validateConfirmPassword();
    const isTermsValid = validateTerms();

    registerButton.disabled = !(isFullNameValid && isEmailValid && isPhoneValid &&
        isPasswordValid && isConfirmPasswordValid && isTermsValid);

    return isFullNameValid && isEmailValid && isPhoneValid &&
        isPasswordValid && isConfirmPasswordValid && isTermsValid;
}

// Initialize login functionality
function initLoginPage() {
    // Password toggle
    $('.password-toggle').click(function () {
        const input = $(this).closest('.input-container').find('input');
        const icon = $(this).find('i');

        if (input.attr('type') === 'password') {
            input.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
            $(this).attr('aria-label', 'Hide password');
        } else {
            input.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
            $(this).attr('aria-label', 'Show password');
        }
    });

    // Form validation
    $('#loginForm').submit(function (e) {
        let isValid = true;

        // Validate email
        const email = $('#Email').val().trim();
        if (!email) {
            $('#emailError').text('Email is required').show();
            isValid = false;
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            $('#emailError').text('Please enter a valid email address').show();
            isValid = false;
        } else {
            $('#emailError').hide();
        }

        // Validate password
        if (!$('#Password').val()) {
            $('#passwordError').text('Password is required').show();
            isValid = false;
        } else {
            $('#passwordError').hide();
        }

        if (!isValid) {
            e.preventDefault();
            // Add shake animation to first invalid field
            if (!$('#Email').val().trim()) {
                $('#Email').addClass('shake');
                setTimeout(() => $('#Email').removeClass('shake'), 500);
            } else if (!$('#Password').val()) {
                $('#Password').addClass('shake');
                setTimeout(() => $('#Password').removeClass('shake'), 500);
            }
        }
    });
}

$(document).ready(function () {
    initLoginPage();

    // Initialize floating labels
    $('.input-container input').each(function () {
        if ($(this).val()) {
            $(this).next('label').addClass('floating');
        }
    }).on('focus', function () {
        $(this).next('label').addClass('floating');
    }).on('blur', function () {
        if (!$(this).val()) {
            $(this).next('label').removeClass('floating');
        }
    });
});
