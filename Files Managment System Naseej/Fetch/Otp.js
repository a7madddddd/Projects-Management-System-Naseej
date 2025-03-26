document.addEventListener('DOMContentLoaded', function () {
    const emailInput = document.getElementById('email');
    const otpSection = document.getElementById('otpSection');
    const sendOtpBtn = document.getElementById('sendOtpBtn');
    const verifyOtpBtn = document.getElementById('verifyOtpBtn');
    const resetPasswordBtn = document.getElementById('resetPasswordBtn');

    let resetToken = null;

    sendOtpBtn.addEventListener('click', async function () {
        const email = emailInput.value.trim();

        try {
            const response = await fetch('/api/Auth/send-reset-otp', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email })
            });

            const result = await response.json();

            if (response.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'OTP Sent',
                    text: result.message
                });
                otpSection.style.display = 'block';
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: result.message
                });
            }
        } catch (error) {
            console.error('Error:', error);
        }
    });

    verifyOtpBtn.addEventListener('click', async function () {
        const email = emailInput.value.trim();
        const otp = document.getElementById('otpInput').value.trim();

        try {
            const response = await fetch('/api/Auth/verify-reset-otp', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email, otp })
            });

            const result = await response.json();

            if (response.ok) {
                resetToken = result.resetToken;
                Swal.fire({
                    icon: 'success',
                    title: 'OTP Verified',
                    text: 'You can now reset your password'
                });
                // Show password reset section
                document.getElementById('passwordResetSection').style.display = 'block';
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: result.message
                });
            }
        } catch (error) {
            console.error('Error:', error);
        }
    });

    resetPasswordBtn.addEventListener('click', async function () {
        const email = emailInput.value.trim();
        const newPassword = document.getElementById('newPassword').value.trim();
        const confirmPassword = document.getElementById('confirmPassword').value.trim();

        if (newPassword !== confirmPassword) {
            Swal.fire({
                icon: 'warning',
                title: 'Password Mismatch',
                text: 'Passwords do not match'
            });
            return;
        }

        try {
            const response = await fetch('/api/Auth/reset-password', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email,
                    newPassword,
                    resetToken
                })
            });

            const result = await response.json();

            if (response.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Password Reset',
                    text: result.message
                }).then(() => {
                    window.location.href = 'auth-login.html';
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: result.message
                });
            }
        } catch (error) {
            console.error('Error:', error);
        }
    });
});