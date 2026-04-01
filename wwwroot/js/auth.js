const AUTH_API = '/api/auth';

function showTab(type) {
    // ДОДАНО: Очищуємо поля обох форм при перемиканні вкладок
    document.getElementById('loginForm').reset();
    document.getElementById('registerForm').reset();
    
    // ДОДАНО: Ховаємо повідомлення про помилки, якщо вони були
    document.getElementById('loginError').classList.add('hidden');
    document.getElementById('registerError').classList.add('hidden');

    const isLogin = type === 'login';
    document.getElementById('loginForm').classList.toggle('hidden', !isLogin);
    document.getElementById('registerForm').classList.toggle('hidden', isLogin);
    document.getElementById('btnTabLogin').classList.toggle('active', isLogin);
    document.getElementById('btnTabRegister').classList.toggle('active', !isLogin);
}

// ОБРОБКА ВХОДУ
document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const errorDiv = document.getElementById('loginError');
    errorDiv.classList.add('hidden');

    const data = {
        email: document.getElementById('loginEmail').value,
        password: document.getElementById('loginPassword').value
    };

    try {
        const response = await fetch(`${AUTH_API}/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (response.ok) {
            // ЗБЕРІГАЄМО ДАНІ (Вимога MVP)
            localStorage.setItem('userId', result.userId);
            localStorage.setItem('userName', result.userName);
            
            // Переходимо на головну сторінку
            window.location.href = 'index.html';
        } else {
            errorDiv.innerText = result || "Помилка входу";
            errorDiv.classList.remove('hidden');
        }
    } catch (err) {
        errorDiv.innerText = "Сервер не відповідає";
        errorDiv.classList.remove('hidden');
    }
});

// ОБРОБКА РЕЄСТРАЦІЇ
document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const errorDiv = document.getElementById('registerError');
    errorDiv.classList.add('hidden');

    const data = {
        fullName: document.getElementById('regFullName').value,
        email: document.getElementById('regEmail').value,
        password: document.getElementById('regPassword').value
    };

    try {
        const response = await fetch(`${AUTH_API}/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (response.ok) {
            alert("Реєстрація успішна! Тепер увійдіть.");
            
            // ДОДАНО: Очищуємо форму реєстрації після успішного створення акаунту
            document.getElementById('registerForm').reset();
            
            showTab('login');
        } else {
            // Обробка помилок валідації (Вимога №10)
            errorDiv.innerText = Array.isArray(result) ? result[0].description : "Помилка реєстрації";
            errorDiv.classList.remove('hidden');
        }
    } catch (err) {
        errorDiv.innerText = "Сервер не відповідає";
        errorDiv.classList.remove('hidden');
    }
});