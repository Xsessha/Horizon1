const API_URL = '/api/events';
let currentDate = new Date();

document.addEventListener('DOMContentLoaded', () => {

    checkAuth();
    renderCalendar();

    document.getElementById('eventForm').addEventListener('submit', createEvent);

    // Додаємо обробники для стрілок календаря
    document.getElementById('prevMonth').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    });
    document.getElementById('nextMonth').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    });

    // Додаємо logout глобально
    window.logout = function() {
        localStorage.removeItem('userId');
        localStorage.removeItem('userName');
        window.location.href = 'login.html';
    }
});

function checkAuth() {
    const userId = localStorage.getItem('userId');
    if (!userId) window.location.href = 'login.html';
}

async function renderCalendar() {
    const grid = document.getElementById('calendarGrid');
    const monthYearLabel = document.getElementById('currentMonthYear');
    
    // Очистка старих днів (залишаємо тільки заголовки Пн-Нд)
    const headers = grid.querySelectorAll('.day-header');
    grid.innerHTML = '';
    headers.forEach(h => grid.appendChild(h));

    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    monthYearLabel.innerText = `${new Intl.DateTimeFormat('uk-UA', { month: 'long', year: 'numeric' }).format(currentDate)}`;

    // Отримання подій з API (Вимога №4)
    const response = await fetch(`${API_URL}/month/${year}/${month + 1}`);
    const events = await response.json();

    // Генерація днів (спрощено)
    for (let i = 1; i <= 30; i++) {
        const daySquare = document.createElement('div');
        daySquare.className = 'calendar-day';
        daySquare.innerHTML = `<span>${i}</span>`;
        daySquare.onclick = () => openModal(i);

        // Відображення подій (Вимога №6)
        const dayEvents = events.filter(e => new Date(e.startTime).getDate() === i);
        dayEvents.forEach(e => {
            const evEl = document.createElement('div');
            evEl.className = 'event-item';
            evEl.style.backgroundColor = e.category?.colorHex || '#999';
            evEl.innerText = e.title;
            daySquare.appendChild(evEl);
        });

        grid.appendChild(daySquare);
    }
}

function openModal(day) {
    document.getElementById('eventModal').style.display = 'block';
}

function closeModal() {
    document.getElementById('eventModal').style.display = 'none';
}

async function createEvent(e) {
    e.preventDefault();
    
    const start = new Date(document.getElementById('startTime').value);
    const end = new Date(document.getElementById('endTime').value);

    // Валідація (Вимога №10)
    if (end <= start) {
        alert("Час завершення має бути пізнішим за початок!");
        return;
    }

    const newEvent = {
        title: document.getElementById('eventTitle').value,
        description: document.getElementById('eventDesc').value,
        startTime: start.toISOString(),
        endTime: end.toISOString(),
        categoryId: parseInt(document.getElementById('categoryId').value)
    };

    const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newEvent)
    });

    if (response.ok) {
        closeModal();
        renderCalendar();
    }
}