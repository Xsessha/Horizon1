const API_URL = '/api/events';
const CATEGORY_API_URL = '/api/categories';
let currentDate = new Date();

async function loadCategories() {
    try {
        const userToken = localStorage.getItem('token');
        const response = await fetch(CATEGORY_API_URL, {
            headers: userToken ? { 'Authorization': `Bearer ${userToken}` } : {}
        });
        const categories = await response.json();

        const categorySelect = document.getElementById('categoryId');
        const categoryList = document.getElementById('categoryList');

        categorySelect.innerHTML = '';
        categoryList.innerHTML = '';

        categories.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            categorySelect.appendChild(opt);

            const li = document.createElement('li');
            li.innerHTML = `<span class="dot" style="background:${c.colorHex}"></span> ${c.name}`;
            categoryList.appendChild(li);
        });

        const customOpt = document.createElement('option');
        customOpt.value = 'other';
        customOpt.textContent = 'Інша';
        categorySelect.appendChild(customOpt);
    } catch (err) {
        console.warn('Не вдалося завантажити категорії, використано дефолтні.', err);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    loadCategories();
    renderCalendar();

    document.getElementById('eventForm').addEventListener('submit', createEvent);

    document.getElementById('categoryId').addEventListener('change', (ev) => {
        const isOther = ev.target.value === 'other';
        document.getElementById('customCategoryWrapper').style.display = isOther ? 'block' : 'none';
    });

    document.getElementById('prevMonth').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    });
    document.getElementById('nextMonth').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    });

    window.logout = function() {
        localStorage.removeItem('userId');
        localStorage.removeItem('userName');
        localStorage.removeItem('token');
        window.location.href = 'login.html';
    }
});

function checkAuth() {
    const userId = localStorage.getItem('userId');
    if (!userId) {
        window.location.href = 'login.html';
        return;
    }

    const userName = localStorage.getItem('userName') || 'Користувач';
    const userNameSpan = document.getElementById('userName');
    if (userNameSpan) {
        userNameSpan.textContent = userName;
    }
}

async function renderCalendar() {
    const grid = document.getElementById('calendarGrid');
    const monthYearLabel = document.getElementById('currentMonthYear');
    const headers = grid.querySelectorAll('.day-header');
    grid.innerHTML = '';
    headers.forEach(h => grid.appendChild(h));

    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    monthYearLabel.innerText = `${new Intl.DateTimeFormat('uk-UA', { month: 'long', year: 'numeric' }).format(currentDate)}`;

    const response = await fetch(`${API_URL}/month/${year}/${month + 1}`);
    const events = await response.json();

    const startDate = new Date(year, month, 1);
    const dayOfWeek = (startDate.getDay() + 6) % 7; // 0=Пн
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    for (let i = 0; i < dayOfWeek; i++) {
        const empty = document.createElement('div');
        empty.className = 'calendar-day empty';
        grid.appendChild(empty);
    }

    for (let day = 1; day <= daysInMonth; day++) {
        const date = new Date(year, month, day);
        const daySquare = document.createElement('div');
        daySquare.className = 'calendar-day';
        daySquare.innerHTML = `<span>${day}</span>`;
        daySquare.onclick = () => openModal(day);

        const dayEvents = events.filter(e => {
            const start = new Date(e.startTime);
            const end = new Date(e.endTime);
            if (e.recurrencePattern) {
                // recurring events уже розгорнуті з бекенду в окремі дні
                return start.getFullYear() === date.getFullYear() &&
                    start.getMonth() === date.getMonth() &&
                    start.getDate() === date.getDate();
            }
            return date >= start && date <= end;
        });

        dayEvents.forEach(e => {
            const evEl = document.createElement('div');
            evEl.className = 'event-item';
            const categoryColor = e.isTemporaryCategory ? e.temporaryCategoryColor || '#999' : e.category?.colorHex || '#999';
            evEl.style.backgroundColor = categoryColor;
            const categoryLabel = e.isTemporaryCategory ? ` (${e.temporaryCategoryName || 'Тимчасова'})` : '';
            evEl.innerText = e.title + categoryLabel;

            const deleteBtn = document.createElement('span');
            deleteBtn.className = 'delete-event-btn';
            deleteBtn.innerHTML = '&times;';

            deleteBtn.onclick = async (eventClick) => {
                eventClick.stopPropagation();
                const isConfirmed = confirm(`Ви дійсно хочете видалити подію "${e.title}"?`);
                if (isConfirmed) {
                    await deleteEventFromServer(e.id, evEl);
                }
            };

            evEl.appendChild(deleteBtn);
            daySquare.appendChild(evEl);
        });

        grid.appendChild(daySquare);
    }
}

// НОВА ФУНКЦІЯ: Відправка запиту на сервер для видалення події
async function deleteEventFromServer(eventId, eventHtmlElement) {
    try {
        const response = await fetch(`${API_URL}/${eventId}`, {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' }
        });

        if (response.ok) {
            eventHtmlElement.remove(); // Видаляємо подію з екрану (DOM), якщо сервер відповів успіхом
        } else {
            const errorText = await response.text();
            alert(`Помилка видалення: ${errorText}`);
        }
    } catch (error) {
        console.error("Помилка під час видалення події:", error);
        alert("Не вдалося з'єднатися з сервером.");
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

    if (end <= start) {
        alert("Час завершення має бути пізнішим за початок!");
        return;
    }

    const selectedCategory = document.getElementById('categoryId').value;
    const isOtherCategory = selectedCategory === 'other';
    const customCategoryName = document.getElementById('customCategoryName').value.trim();

    if (isOtherCategory && !customCategoryName) {
        alert('Будь ласка, вкажіть назву для власної категорії.');
        return;
    }

    const newEvent = {
        title: document.getElementById('eventTitle').value,
        description: document.getElementById('eventDesc').value,
        startTime: start.toISOString(),
        endTime: end.toISOString(),
        isTemporaryCategory: isOtherCategory,
        temporaryCategoryName: isOtherCategory ? customCategoryName : null,
        temporaryCategoryColor: isOtherCategory ? document.getElementById('customCategoryColor').value : null,
        categoryId: isOtherCategory ? null : parseInt(selectedCategory),
        recurrencePattern: parseInt(document.getElementById('recurrencePattern').value)
    };

    const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newEvent)
    });

    if (response.ok) {
        closeModal();
        renderCalendar();
    } else {
        const err = await response.text();
        alert(`Помилка при створенні події: ${err}`);
    }
}