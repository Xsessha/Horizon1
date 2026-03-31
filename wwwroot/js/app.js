const API_URL = '/api/events';
let currentDate = new Date();

document.addEventListener('DOMContentLoaded', () => {

    checkAuth();
    loadCategories();
    renderCalendar();

    // Встановлюємо ім'я користувача в header
    const userName = localStorage.getItem('userName');
    if (userName) {
        const userNameSpan = document.getElementById('userName');
        if (userNameSpan) userNameSpan.textContent = userName;
    }

    document.getElementById('eventForm').addEventListener('submit', createEvent);
    document.getElementById('addCategoryBtn').addEventListener('click', openCategoryModal);
    document.getElementById('categoryForm').addEventListener('submit', createCategory);
    document.getElementById('isRecurring').addEventListener('change', toggleRecurrenceOptions);

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
    const startOfMonth = new Date(year, month, 1);
    const endOfMonth = new Date(year, month + 1, 0);
    const response = await fetch(`${API_URL}/daterange?start=${startOfMonth.toISOString()}&end=${endOfMonth.toISOString()}`);
    const events = await response.json();

    // --- Синхронізація з реальним календарем (початок тижня — понеділок) ---
    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);
    // 0 - неділя, 1 - понеділок ...
    let startDay = firstDayOfMonth.getDay();
    if (startDay === 0) startDay = 7; // щоб понеділок був першим
    const startDate = new Date(firstDayOfMonth);
    startDate.setDate(startDate.getDate() - (startDay - 1));

    // Генерація днів (6 тижнів для повного відображення)
    for (let week = 0; week < 6; week++) {
        for (let dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++) {
            const currentDay = new Date(startDate);
            currentDay.setDate(startDate.getDate() + (week * 7) + dayOfWeek);

            const daySquare = document.createElement('div');
            daySquare.className = 'calendar-day';
            if (currentDay.getMonth() !== month) {
                daySquare.classList.add('other-month');
            }
            daySquare.innerHTML = `<span>${currentDay.getDate()}</span>`;
            daySquare.onclick = () => openModal(currentDay);

            // Відображення подій (Вимога №6)
            const dayEvents = events.filter(e => {
                const eventStart = new Date(e.startTime);
                const eventEnd = new Date(e.endTime);
                return eventStart.toDateString() === currentDay.toDateString() ||
                       (eventStart <= currentDay && eventEnd >= currentDay);
            });

            dayEvents.forEach(e => {
                const evEl = document.createElement('div');
                evEl.className = 'event-item';
                // Пастельний колір для категорії (якщо є)
                let pastel = '#b6e2d3';
                if (e.category && e.category.colorHex) {
                    // Можна зробити мапу кольорів для різних категорій
                    pastel = e.category.colorHex;
                }
                evEl.style.backgroundColor = pastel;
                evEl.innerText = e.title;

                // --- Хрестик ---
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

async function loadCategories() {
    try {
        const response = await fetch('/api/categories');
        const categories = await response.json();
        
        const categorySelect = document.getElementById('categoryId');
        categorySelect.innerHTML = '';
        
        categories.forEach(cat => {
            const option = document.createElement('option');
            option.value = cat.id;
            option.textContent = cat.name;
            categorySelect.appendChild(option);
        });
        
        // Update sidebar
        const categoryList = document.getElementById('categoryList');
        categoryList.innerHTML = '';
        
        categories.forEach(cat => {
            const li = document.createElement('li');
            li.innerHTML = `<span class="dot" style="background:${cat.colorHex}"></span> ${cat.name}`;
            categoryList.appendChild(li);
        });
        
    } catch (error) {
        console.error('Error loading categories:', error);
    }
}

function toggleRecurrenceOptions() {
    const isRecurring = document.getElementById('isRecurring').checked;
    const recurrenceType = document.getElementById('recurrenceType');
    const recurrenceEndDate = document.getElementById('recurrenceEndDate');
    
    recurrenceType.disabled = !isRecurring;
    recurrenceEndDate.disabled = !isRecurring;
}

function openCategoryModal() {
    document.getElementById('categoryModal').classList.add('active');
}

function closeCategoryModal() {
    document.getElementById('categoryModal').classList.remove('active');
}

async function createCategory(e) {
    e.preventDefault();
    
    const category = {
        name: document.getElementById('categoryName').value,
        colorHex: document.getElementById('categoryColor').value
    };
    
    try {
        const response = await fetch('/api/categories', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(category)
        });
        
        if (response.ok) {
            closeCategoryModal();
            loadCategories();
            document.getElementById('categoryForm').reset();
        } else {
            alert('Помилка створення категорії');
        }
    } catch (error) {
        console.error('Error creating category:', error);
        alert('Помилка створення категорії');
    }
}

function closeModal() {
    document.getElementById('eventModal').style.display = 'none';
}

async function createEvent(e) {
    e.preventDefault();
    
    const start = new Date(document.getElementById('startTime').value);
    const endValue = document.getElementById('endTime').value;
    let end = null;
    if (endValue && !isNaN(Date.parse(endValue))) {
        end = new Date(endValue);
        if (end <= start) {
            alert("Час завершення має бути пізнішим за початок!");
            return;
        }
    }
    
    // Перевірка назви
    const titleValue = document.getElementById('eventTitle').value.trim();
    if (titleValue.length < 3 || titleValue.length > 100) {
        alert("Назва має бути від 3 до 100 символів");
        return;
    }
    
    const isRecurring = document.getElementById('isRecurring').checked;
    const enableReminder = document.getElementById('enableReminder').checked;
    
    // Формуємо об'єкт події
    const newEvent = {
        Title: titleValue,
        Description: document.getElementById('eventDesc').value,
        StartTime: start.toISOString(),
        CategoryId: parseInt(document.getElementById('categoryId').value),
        IsRecurring: isRecurring,
        RecurrenceType: isRecurring ? parseInt(document.getElementById('recurrenceType').value) : 0,
        RecurrenceEndDate: isRecurring ? document.getElementById('recurrenceEndDate').value : null
    };
    // Додаємо EndTime тільки якщо воно є
    if (end) {
        newEvent.EndTime = end.toISOString();
    }
    
        url += '?' + params.toString();
    }

    const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (response.ok) {
        closeModal();
        renderCalendar();
        document.getElementById('eventForm').reset();
        document.getElementById('isRecurring').checked = false;
        toggleRecurrenceOptions();
    } else {
        const error = await response.text();
        alert(`Помилка: ${error}`);
    }
}

function openModal(selectedDate) {
    const modal = document.getElementById('eventModal');
    modal.classList.add('active');
    if (selectedDate instanceof Date) {
        const startTime = new Date(selectedDate);
        startTime.setHours(9, 0, 0, 0);
        document.getElementById('startTime').value = startTime.toISOString().slice(0, 16);
        document.getElementById('endTime').value = '';
    }
}

function closeModal() {
    document.getElementById('eventModal').classList.remove('active');
}