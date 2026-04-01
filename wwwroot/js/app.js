const API_URL = '/api/events';
const CATEGORY_API_URL = '/api/categories';
let currentDate = new Date();

function getAuthHeaders() {
    const token = localStorage.getItem('token');
    return token ? { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` } : { 'Content-Type': 'application/json' };
}

async function loadCategories() {
    try {
        const response = await fetch('/api/categories', {
            headers: getAuthHeaders() // Використовуємо твою функцію для токена
        });

        if (!response.ok) return;

        const categories = await response.json();
        const categorySelect = document.getElementById('categoryId');
        const categoryList = document.getElementById('categoryList');

        if (!categoryList || !categorySelect) return;

        categorySelect.innerHTML = '';
        categoryList.innerHTML = '';

        categories.forEach(c => {
            // 1. Додаємо у випадаючий список форми
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            categorySelect.appendChild(opt);

            // 2. Малюємо красиву картку в сайдбарі
            const div = document.createElement('div');
            div.className = 'category-item';
            div.innerHTML = `
                <div class="category-marker" style="background-color: ${c.colorHex}"></div>
                <span class="category-label">${c.name}</span>
            `;
            
            // Додаємо обробку кліку (фільтрація)
            div.onclick = () => console.log(`Фільтр по категорії: ${c.name}`);
            
            categoryList.appendChild(div);
        });

        // Додаємо опцію "Інша" в кінець списку
        const otherOpt = document.createElement('option');
        otherOpt.value = 'other';
        otherOpt.textContent = 'Інша';
        categorySelect.appendChild(otherOpt);

    } catch (err) {
        console.error('Помилка завантаження категорій:', err);
    }
}

// Виклик функції при старті
document.addEventListener('DOMContentLoaded', loadCategories);

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
    document.getElementById('recurrencePattern').addEventListener('change', (ev) => {
        const pattern = parseInt(ev.target.value);
        document.getElementById('recurrenceEndDateWrapper').style.display = (pattern !== 0 && pattern !== 4) ? 'block' : 'none';
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

    let events = [];
    try {
        // Завантажуємо події за поточний, минулий та наступний місяці разом
        const responses = await Promise.all([
            fetch(`${API_URL}/month/${year}/${month}`, { headers: getAuthHeaders() }),     // минулий
            fetch(`${API_URL}/month/${year}/${month + 1}`, { headers: getAuthHeaders() }), // поточний
            fetch(`${API_URL}/month/${year}/${month + 2}`, { headers: getAuthHeaders() })  // наступний
        ]);

        for (const response of responses) {
            if (response.ok) {
                const data = await response.json();
                events = events.concat(data);
            }
        }
    } catch (error) {
        console.error('Помилка завантаження подій:', error);
    }

    const startDate = new Date(year, month, 1);
    const dayOfWeek = (startDate.getDay() + 6) % 7; // 0=Пн
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    // --- 1. ДНІ МИНУЛОГО МІСЯЦЯ ---
    for (let i = dayOfWeek; i > 0; i--) {
        const day = daysInPrevMonth - i + 1;
        const date = new Date(year, month - 1, day);
        createDaySquare(grid, day, date, true, events); 
    }

    // --- 2. ДНІ ПОТОЧНОГО МІСЯЦЯ ---
    for (let day = 1; day <= daysInMonth; day++) {
        const date = new Date(year, month, day);
        createDaySquare(grid, day, date, false, events);
    }

    // --- 3. ДНІ НАСТУПНОГО МІСЯЦЯ ---
    const totalCells = 42;
    const currentCells = grid.children.length - 7;
    for (let day = 1; day <= (totalCells - currentCells); day++) {
        const date = new Date(year, month + 1, day);
        createDaySquare(grid, day, date, true, events);
    }
}

function createDaySquare(grid, day, date, isOtherMonth, events) {
    const daySquare = document.createElement('div');
    daySquare.className = isOtherMonth ? 'calendar-day other-month' : 'calendar-day';
    daySquare.innerHTML = `<span>${day}</span>`;
    
    if (!isOtherMonth) {
        daySquare.onclick = () => openModal(day);
    }

    const dayEvents = events.filter(e => {
        const start = new Date(e.startTime);
        const end = e.endTime ? new Date(e.endTime) : new Date(start);
        const eventStartDate = new Date(start.getFullYear(), start.getMonth(), start.getDate());
        const eventEndDate = new Date(end.getFullYear(), end.getMonth(), end.getDate());
        return date >= eventStartDate && date <= eventEndDate;
    });

    dayEvents.forEach(e => {
        const evEl = document.createElement('div');
        evEl.className = 'event-item';
        evEl.dataset.eventId = e.id;
        
        const categoryColor = e.isTemporaryCategory ? e.temporaryCategoryColor || '#999' : e.category?.colorHex || '#999';
        evEl.style.backgroundColor = categoryColor;
        const categoryLabel = e.isTemporaryCategory ? ` (${e.temporaryCategoryName || 'Тимчасова'})` : '';
        evEl.innerText = e.title + categoryLabel;

        // --- НОВИЙ БЛОК: КЛІК ДЛЯ РЕДАГУВАННЯ ---
        evEl.onclick = (eventClick) => {
            eventClick.stopPropagation(); // Зупиняємо, щоб не відкрилася порожня модалка дня
            openEditModal(e); // Викликаємо функцію редагування і передаємо всю подію
        };

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

// НОВА ФУНКЦІЯ: Відправка запиту на сервер для видалення події
async function deleteEventFromServer(eventId, eventHtmlElement) {
    try {
        const response = await fetch(`${API_URL}/${eventId}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            const elementsToRemove = document.querySelectorAll(`[data-event-id="${eventId}"]`);
            elementsToRemove.forEach(el => el.remove());
        } else {
            const errorText = await response.text();
            alert(`Помилка видалення: ${errorText}`);
        }
    } catch (error) {
        console.error("Помилка під час видалення події:", error);
        alert("Не вдалося з'єднатися з сервером.");
    }
}

function formatLocalDateTime(date) {
    const pad = (n) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function openModal(day) {
    const selectedDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), day);
    const startTimeInput = document.getElementById('startTime');
    const endTimeInput = document.getElementById('endTime');

    const startDateTime = new Date(selectedDate);
    startDateTime.setHours(9, 0, 0, 0);
    const endDateTime = new Date(startDateTime);
    endDateTime.setHours(10, 0, 0, 0);

    startTimeInput.value = formatLocalDateTime(startDateTime);
    endTimeInput.value = formatLocalDateTime(endDateTime);

    document.getElementById('eventTitle').value = '';
    document.getElementById('eventDesc').value = '';
    document.getElementById('recurrencePattern').value = '0';
    document.getElementById('categoryId').value = '1';
    document.getElementById('customCategoryWrapper').style.display = 'none';
    document.getElementById('customCategoryName').value = '';
    document.getElementById('customCategoryColor').value = '#ff9900';

    document.getElementById('eventModal').style.display = 'block';
    document.getElementById('recurrenceEndDateWrapper').style.display = 'none';
    document.getElementById('recurrenceEndDate').value = '';
}

function closeModal() {
    document.getElementById('eventModal').style.display = 'none';
    document.getElementById('eventForm').reset();
    
    // ВАЖЛИВО: Очищаємо ID редагування та повертаємо заголовок
    document.getElementById('editEventId').value = "";
    document.getElementById('modalTitle').innerText = "Нова подія";
}

async function createEvent(e) {
    e.preventDefault();
    
    // ПЕРЕВІРКА: чи ми редагуємо (якщо в прихованому полі є ID)
    const editId = document.getElementById('editEventId').value;
    const isEdit = editId !== ""; 

    const startValue = document.getElementById('startTime').value;
    const endValue = document.getElementById('endTime').value;
    const start = new Date(startValue);
    const end = new Date(endValue);

    if (endValue && end <= start) {
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

    const recurrencePatternValue = parseInt(document.getElementById('recurrencePattern').value);
    const recurrenceEndDateValue = document.getElementById('recurrenceEndDate').value;

    const eventData = {
        title: document.getElementById('eventTitle').value,
        description: document.getElementById('eventDesc').value,
        startTime: startValue,
        endTime: endValue,
        isTemporaryCategory: isOtherCategory,
        temporaryCategoryName: isOtherCategory ? customCategoryName : null,
        temporaryCategoryColor: isOtherCategory ? document.getElementById('customCategoryColor').value : null,
        categoryId: isOtherCategory ? null : parseInt(selectedCategory),
        recurrencePattern: recurrencePatternValue,
        recurrenceEndDate: (recurrencePatternValue !== 0 && recurrencePatternValue !== 4 && recurrenceEndDateValue) ? recurrenceEndDateValue : null
    };

    // ВИЗНАЧАЄМО МЕТОД ТА URL (PUT для редагування, POST для нового)
    const url = isEdit ? `${API_URL}/${editId}` : API_URL;
    const method = isEdit ? 'PUT' : 'POST';

    const response = await fetch(url, {
        method: method,
        headers: getAuthHeaders(),
        body: JSON.stringify(eventData)
    });

    if (response.ok) {
        closeModal();
        renderCalendar();
    } else {
        const err = await response.text();
        alert(`Помилка: ${err}`);
    }
}

function openEditModal(eventData) {
    // 1. Міняємо заголовок і показуємо модалку
    document.getElementById('modalTitle').innerText = "Редагувати подію";
    document.getElementById('editEventId').value = eventData.id;

    // 2. Заповнюємо поля даними з бази
    document.getElementById('eventTitle').value = eventData.title;
    document.getElementById('eventDesc').value = eventData.description || '';
    document.getElementById('startTime').value = formatLocalDateTime(new Date(eventData.startTime));
    document.getElementById('endTime').value = formatLocalDateTime(new Date(eventData.endTime));
    
    // Категорія
    const categorySelect = document.getElementById('categoryId');
    if (eventData.isTemporaryCategory) {
        categorySelect.value = 'other';
        document.getElementById('customCategoryWrapper').style.display = 'block';
        document.getElementById('customCategoryName').value = eventData.temporaryCategoryName;
        document.getElementById('customCategoryColor').value = eventData.temporaryCategoryColor;
    } else {
        categorySelect.value = eventData.categoryId;
        document.getElementById('customCategoryWrapper').style.display = 'none';
    }
    document.getElementById('recurrencePattern').value = eventData.recurrencePattern || '0';
    
    // Перевіряємо, чи є повторення і чи воно не дорівнює 4 (Щороку)
    if (eventData.recurrencePattern && eventData.recurrencePattern !== 0 && eventData.recurrencePattern !== 4) {
        document.getElementById('recurrenceEndDateWrapper').style.display = 'block';
        document.getElementById('recurrenceEndDate').value = eventData.recurrenceEndDate ? eventData.recurrenceEndDate.split('T')[0] : '';
    } else {
        document.getElementById('recurrenceEndDateWrapper').style.display = 'none';
        document.getElementById('recurrenceEndDate').value = '';
    }
    document.getElementById('eventModal').style.display = 'block';
    
}