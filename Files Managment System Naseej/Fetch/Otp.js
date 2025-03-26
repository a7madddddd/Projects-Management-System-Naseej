document.getElementById('resetButton').addEventListener('click', function () {
    const email = document.getElementById('userEmail').value;

    // Basic email check
    if (!email) {
        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: 'Please enter your email address!'
        });
        return;
    }

    // Send OTP request
    fetch('https://localhost:44320/api/Account/send-reset-otp', {
        method: 'POST',
        headers: {
            'Accept': '*/*',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            email: email
        })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            if (data.message === "OTP sent successfully") {
                // OTP Input Modal
                Swal.fire({
                    title: 'Enter OTP',
                    html: `
                    <div class="d-flex justify-content-center">
                        <input type="text" id="otp1" maxlength="1" class="form-control otp-input" pattern="\\d*" inputmode="numeric">
                        <input type="text" id="otp2" maxlength="1" class="form-control otp-input" pattern="\\d*" inputmode="numeric">
                        <input type="text" id="otp3" maxlength="1" class="form-control otp-input" pattern="\\d*" inputmode="numeric">
                        <input type="text" id="otp4" maxlength="1" class="form-control otp-input" pattern="\\d*" inputmode="numeric">
                        <input type="text" id="otp5" maxlength="1" class="form-control otp-input" pattern="\\d*" inputmode="numeric">
                        <input type="text" id="otp6" maxlength="1" class="form-control otp-input" pattern="\\d*" inputmode="numeric">
                    </div>
                    <p class="mt-3 text-muted">Please enter the 6-digit code sent to your email</p>
                `,
                    confirmButtonText: 'Verify OTP',
                    focusConfirm: false,
                    preConfirm: () => {
                        const otp1 = document.getElementById('otp1').value;
                        const otp2 = document.getElementById('otp2').value;
                        const otp3 = document.getElementById('otp3').value;
                        const otp4 = document.getElementById('otp4').value;
                        const otp5 = document.getElementById('otp5').value;
                        const otp6 = document.getElementById('otp6').value;

                        const otp = otp1 + otp2 + otp3 + otp4 + otp5 + otp6;

                        if (otp.length !== 6) {
                            Swal.showValidationMessage('Please enter all 6 digits');
                            return false;
                        }

                        // Verify OTP
                        return fetch('https://localhost:44320/api/Account/verify-reset-otp', {
                            method: 'POST',
                            headers: {
                                'Accept': '*/*',
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify({
                                email: email,
                                otp: otp
                            })
                        })
                            .then(response => response.json())
                            .then(verifyData => {
                                if (verifyData.message === "OTP verified successfully") {
                                    // Get user ID
                                    return fetch('https://localhost:44320/api/User/get-user-id', {
                                        method: 'POST',
                                        headers: {
                                            'Accept': '*/*',
                                            'Content-Type': 'application/json'
                                        },
                                        body: JSON.stringify({
                                            email: email
                                        })
                                    })
                                        .then(response => response.json())
                                        .then(userData => ({
                                            userId: userData.userId,
                                            otpVerified: true
                                        }));
                                } else {
                                    throw new Error(verifyData.message || 'OTP verification failed');
                                }
                            });
                    },
                    willOpen: () => {
                        // OTP input navigation logic
                        const otpInputs = document.querySelectorAll('.otp-input');
                        otpInputs.forEach((input, index) => {
                            input.addEventListener('input', (e) => {
                                e.target.value = e.target.value.replace(/[^0-9]/g, '');
                                if (e.target.value.length === 1 && index < otpInputs.length - 1) {
                                    otpInputs[index + 1].focus();
                                }
                            });

                            input.addEventListener('keydown', (e) => {
                                if (e.key === 'Backspace' && e.target.value.length === 0 && index > 0) {
                                    otpInputs[index - 1].focus();
                                }
                            });
                        });

                        otpInputs[0].focus();
                    }
                }).then((result) => {
                    if (result.isConfirmed && result.value.otpVerified) {
                        // Prompt for new password
                        return Swal.fire({
                            title: 'Reset Password',
                            html: `
                            <input type="password" id="newPassword" class="swal2-input" placeholder="New Password">
                            <input type="password" id="confirmPassword" class="swal2-input" placeholder="Confirm New Password">
                        `,
                            confirmButtonText: 'Reset Password',
                            focusConfirm: false,
                            preConfirm: () => {
                                const newPassword = document.getElementById('newPassword').value;
                                const confirmPassword = document.getElementById('confirmPassword').value;

                                // Simple password match validation
                                if (newPassword !== confirmPassword) {
                                    Swal.showValidationMessage('Passwords do not match');
                                    return false;
                                }

                                // Reset Password Request (using new OTP reset endpoint)
                                return fetch('https://localhost:44320/api/User/otp-reset-password', {
                                    method: 'POST',
                                    headers: {
                                        'Accept': '*/*',
                                        'Content-Type': 'application/json'
                                    },
                                    body: JSON.stringify({
                                        userId: result.value.userId,
                                        newPassword: newPassword,
                                        confirmPassword: confirmPassword
                                    })
                                })
                                    .then(response => {
                                        if (!response.ok) {
                                            return response.text().then(text => {
                                                console.error('Full error response:', text);
                                                throw new Error(text || 'Password reset failed');
                                            });
                                        }
                                        return response.json();
                                    });
                            }
                        });
                    }
                }).then((result) => {
                    if (result && result.isConfirmed) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Password Reset',
                            text: 'Your password has been successfully reset!'
                        }).then(() => {
                            window.location.href = "auth-login.html";
                        });
                    }
                }).catch((error) => {
                    Swal.fire({
                        icon: 'error',
                        title: 'Reset Failed',
                        text: error.message || 'Unable to reset password'
                    });
                    console.error('Password Reset Error:', error);
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Unable to send OTP. Please try again.'
                });
            }
        })
        .catch(error => {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: `There was a problem sending the OTP: ${error.message}`
            });
            console.error('Error:', error);
        });
});