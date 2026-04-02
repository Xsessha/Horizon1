const API_URL = '/api/events';
const CATEGORY_API_URL = '/api/categories';
let currentDate = new Date();

// --- ДОПОМІЖНІ ФУНКЦІЇ ---

function getAuthHeaders() {
    const token = localStorage.getItem('token');
    return token ? { 
        'Content-Type': 'application/json', 
        'Authorization': `Bearer ${token}` 
    } : { 'Content-Type': 'application/json' };
}

function formatLocalDateTime(date) {
    const d = new Date(date);
    const pad = (n) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

// --- РОБОТА З КОРИСТУВАЧЕМ (ДОДАНО) ---

function initializeUserInfo() {
    const userName = localStorage.getItem('userName');
    const userInfoEl = document.getElementById('userInfo');
    if (userName && userInfoEl) {
        userInfoEl.innerText = userName; // Замінюємо слово "Користувач" на ім'я
    }
}

function logout() {
    // Очищуємо всі збережені дані при виході
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('userName');
    window.location.href = 'login.html'; // Перекидаємо на сторінку входу
}

// --- КАТЕГОРІЇ ---

async function loadCategories() {
    try {
        const response = await fetch(CATEGORY_API_URL, { headers: getAuthHeaders() });
        if (!response.ok) return;

        const categories = await response.json();
        const categorySelect = document.getElementById('categoryId');
        const categoryList = document.getElementById('categoryList');

        if (!categoryList || !categorySelect) return;

        categorySelect.innerHTML = '';
        categoryList.innerHTML = '';

        categories.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            categorySelect.appendChild(opt);

            const div = document.createElement('div');
            div.className = 'category-item';
            div.innerHTML = `
                <div class="category-marker" style="background-color: ${c.colorHex}"></div>
                <span class="category-label">${c.name}</span>
            `;
            categoryList.appendChild(div);
        });

        const otherOpt = document.createElement('option');
        otherOpt.value = 'other';
        otherOpt.textContent = 'Інша (Власний колір)';
        categorySelect.appendChild(otherOpt);
    } catch (err) {
        console.error('Помилка категорій:', err);
    }
}

// --- ГОЛОВНА ЛОГІКА КАЛЕНДАРЯ ---

async function renderCalendar() {
    const grid = document.getElementById('calendarGrid');
    const monthYearLabel = document.getElementById('currentMonthYear');
    if (!grid) return;

    const headers = grid.querySelectorAll('.day-header');
    grid.innerHTML = '';
    headers.forEach(h => grid.appendChild(h));

    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    monthYearLabel.innerText = `${new Intl.DateTimeFormat('uk-UA', { month: 'long', year: 'numeric' }).format(currentDate)}`;

    let events = [];
    try {
        const responses = await Promise.all([
            fetch(`${API_URL}/month/${year}/${month}`, { headers: getAuthHeaders() }),     
            fetch(`${API_URL}/month/${year}/${month + 1}`, { headers: getAuthHeaders() }), 
            fetch(`${API_URL}/month/${year}/${month + 2}`, { headers: getAuthHeaders() })  
        ]);

        for (const res of responses) {
            if (res.ok) {
                const data = await res.json();
                events = events.concat(data);
            }
        }
    } catch (error) {
        console.error('Помилка завантаження подій:', error);
    }

    const startDate = new Date(year, month, 1);
    const dayOfWeek = (startDate.getDay() + 6) % 7; 
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    for (let i = dayOfWeek; i > 0; i--) {
        const day = daysInPrevMonth - i + 1;
        createDaySquare(grid, day, new Date(year, month - 1, day), true, events);
    }

    for (let day = 1; day <= daysInMonth; day++) {
        createDaySquare(grid, day, new Date(year, month, day), false, events);
    }

    const totalCells = 42;
    const currentCells = grid.children.length - 7;
    for (let day = 1; day <= (totalCells - currentCells); day++) {
        createDaySquare(grid, day, new Date(year, month + 1, day), true, events);
    }

    renderDailySchedule(currentDate, events);
}

function createDaySquare(grid, day, date, isOtherMonth, events) {
    const daySquare = document.createElement('div');
    daySquare.className = isOtherMonth ? 'calendar-day other-month' : 'calendar-day';
    
    const checkDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    daySquare.innerHTML = `<span class="day-number">${day}</span>`;
    
    // Тепер клік по клітинці ТІЛЬКИ оновлює розклад справа
    daySquare.onclick = () => {
        // 1. Оновлюємо розклад для вибраної дати (використовуємо checkDate, яка вже є у функції)
        renderDailySchedule(checkDate, events);
        
        // 2. Знімаємо підсвітку з усіх інших днів
        document.querySelectorAll('.calendar-day').forEach(d => {
            d.classList.remove('active-day');
        });
        
        // 3. Підсвічуємо день, на який щойно клікнули
        daySquare.classList.add('active-day');
    };

    const dayEvents = events.filter(e => {
        const start = new Date(e.startTime);
        const end = new Date(e.endTime);
        const eventStart = new Date(start.getFullYear(), start.getMonth(), start.getDate());
        const eventEnd = new Date(end.getFullYear(), end.getMonth(), end.getDate());
        return checkDate >= eventStart && checkDate <= eventEnd;
    });

    dayEvents.forEach(e => {
        const evEl = document.createElement('div');
        evEl.className = 'event-item';
        
        const color = e.isTemporaryCategory ? 
            (e.temporaryCategoryColor || '#b8c0ff') : 
            (e.category ? e.category.colorHex : '#b8c0ff');
        
        evEl.style.backgroundColor = color;
        evEl.innerText = e.title;

        evEl.onclick = (eventClick) => {
            eventClick.stopPropagation();
            openEditModal(e);
        };

        const deleteBtn = document.createElement('span');
        deleteBtn.className = 'delete-event-btn';
        deleteBtn.innerHTML = '&times;';
        deleteBtn.onclick = async (evClick) => {
            evClick.stopPropagation();
            if (confirm(`Видалити "${e.title}"?`)) {
                const res = await fetch(`${API_URL}/${e.id}`, { method: 'DELETE', headers: getAuthHeaders() });
                if (res.ok) renderCalendar();
            }
        };

        evEl.appendChild(deleteBtn);
        daySquare.appendChild(evEl);
    });

    grid.appendChild(daySquare);
}

// --- МОДАЛЬНІ ВІКНА ---

function closeModal() {
    document.getElementById('eventModal').style.display = 'none';
    document.getElementById('eventForm').reset();
}

function openModal(day) {
    const selectedDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), day);
    document.getElementById('editEventId').value = "";
    document.getElementById('modalTitle').innerText = "Нова подія";
    document.getElementById('eventForm').reset();
    
    document.getElementById('startTime').value = formatLocalDateTime(new Date(selectedDate.setHours(9,0)));
    document.getElementById('endTime').value = formatLocalDateTime(new Date(selectedDate.setHours(10,0)));
    document.getElementById('recurrenceEndDateWrapper').style.display = 'none';
    document.getElementById('eventModal').style.display = 'block';
}

function openEditModal(e) {
    document.getElementById('modalTitle').innerText = "Редагувати подію";
    document.getElementById('editEventId').value = e.id;
    document.getElementById('eventTitle').value = e.title;
    document.getElementById('eventDesc').value = e.description || '';
    document.getElementById('startTime').value = formatLocalDateTime(e.startTime);
    document.getElementById('endTime').value = formatLocalDateTime(e.endTime);
    
    const pattern = e.recurrencePattern || 0;
    document.getElementById('recurrencePattern').value = pattern;
    document.getElementById('recurrenceEndDateWrapper').style.display = (pattern !== 0 && pattern !== 4) ? 'block' : 'none';
    document.getElementById('recurrenceEndDate').value = e.recurrenceEndDate ? e.recurrenceEndDate.split('T')[0] : '';

    if (e.isTemporaryCategory) {
        document.getElementById('categoryId').value = 'other';
        document.getElementById('customCategoryWrapper').style.display = 'block';
        document.getElementById('customCategoryName').value = e.temporaryCategoryName;
        document.getElementById('customCategoryColor').value = e.temporaryCategoryColor;
    } else {
        document.getElementById('categoryId').value = e.categoryId;
        document.getElementById('customCategoryWrapper').style.display = 'none';
    }
    document.getElementById('eventModal').style.display = 'block';
}

// --- ЗБЕРЕЖЕННЯ ---

async function saveEvent(e) {
    e.preventDefault();
    const editId = document.getElementById('editEventId').value;
    const isOther = document.getElementById('categoryId').value === 'other';

    const eventData = {
        title: document.getElementById('eventTitle').value,
        description: document.getElementById('eventDesc').value,
        startTime: document.getElementById('startTime').value,
        endTime: document.getElementById('endTime').value,
        isTemporaryCategory: isOther,
        temporaryCategoryName: isOther ? document.getElementById('customCategoryName').value : null,
        temporaryCategoryColor: isOther ? document.getElementById('customCategoryColor').value : null,
        categoryId: isOther ? null : parseInt(document.getElementById('categoryId').value),
        recurrencePattern: parseInt(document.getElementById('recurrencePattern').value),
        recurrenceEndDate: document.getElementById('recurrenceEndDate').value || null
    };

    const res = await fetch(editId ? `${API_URL}/${editId}` : API_URL, {
        method: editId ? 'PUT' : 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify(eventData)
    });

    if (res.ok) {
        closeModal();
        renderCalendar();
    }
}

// --- ПЛАНИ НА ДЕНЬ ---

function renderDailySchedule(dateToRender, allEvents) {
    const headerEl = document.getElementById('dailyDateHeader');
    const listEl = document.getElementById('dailyEventsList');
    if (!headerEl || !listEl) return;

    // 1. Форматуємо заголовок дати та додаємо кнопку "+"
    const today = new Date();
    const isToday = dateToRender.getDate() === today.getDate() && 
                    dateToRender.getMonth() === today.getMonth() && 
                    dateToRender.getFullYear() === today.getFullYear();
    
    const options = { day: 'numeric', month: 'long' };
    const formattedDate = new Intl.DateTimeFormat('uk-UA', options).format(dateToRender);
    
    // Вставляємо текст і кнопку (тут використовуємо зворотні лапки ` `)
    headerEl.innerHTML = `
        <span class="schedule-date-text">${isToday ? `Сьогодні, ${formattedDate}` : formattedDate}</span>
        <button id="addEventFromSchedule" class="add-schedule-btn" title="Додати подію">+</button>
    `;

    // Вішаємо подію на плюсик, щоб він відкривав вікно для вибраного дня
    const addBtn = document.getElementById('addEventFromSchedule');
    if (addBtn) {
        addBtn.onclick = (event) => {
            event.preventDefault();
            openModal(dateToRender.getDate()); 
        };
    }

    // 2. Фільтруємо події САМЕ для цього дня
    const dayStart = new Date(dateToRender.getFullYear(), dateToRender.getMonth(), dateToRender.getDate());
    const dayEvents = allEvents.filter(e => {
        const start = new Date(e.startTime);
        const end = new Date(e.endTime);
        const evStart = new Date(start.getFullYear(), start.getMonth(), start.getDate());
        const evEnd = new Date(end.getFullYear(), end.getMonth(), end.getDate());
        return dayStart >= evStart && dayStart <= evEnd;
    });

    // 3. Сортуємо події хронологічно (за часом початку)
    dayEvents.sort((a, b) => new Date(a.startTime) - new Date(b.startTime));

    // 4. Очищуємо список
    listEl.innerHTML = '';

    // 5. Виводимо події або повідомлення, якщо їх немає
    if (dayEvents.length === 0) {
        listEl.innerHTML = '<div class="no-events-msg">Немає планів на цей день</div>';
        return;
    }

    dayEvents.forEach(e => {
        const startDate = new Date(e.startTime);
        const endDate = new Date(e.endTime);
        
        // Форматуємо час (наприклад: 09:00 - 10:30)
        const timeString = `${startDate.getHours().toString().padStart(2, '0')}:${startDate.getMinutes().toString().padStart(2, '0')} - ${endDate.getHours().toString().padStart(2, '0')}:${endDate.getMinutes().toString().padStart(2, '0')}`;
        
        const color = e.isTemporaryCategory ? (e.temporaryCategoryColor || '#b8c0ff') : (e.category ? e.category.colorHex : '#b8c0ff');
        const catName = e.isTemporaryCategory ? (e.temporaryCategoryName || 'Інша') : (e.category ? e.category.name : 'Без категорії');

        const itemDiv = document.createElement('div');
        itemDiv.className = 'daily-event-item';
        itemDiv.innerHTML = `
        <div style="display: flex; align-items: flex-start; gap: 10px;">
            <div class="event-dot" style="background-color: ${color}"></div>
            <div class="event-details">
                <span class="event-time">${timeString}</span>
                <span class="event-title">${e.title}</span>
                <span class="event-category-name">${catName}</span>
            </div>
        </div>
        <div class="event-actions">
            <span class="action-icon edit-icon" title="Редагувати">✎</span>
            <span class="action-icon delete-icon" title="Видалити">✖</span>
        </div>
        `;
        
        // 1. РОБОЧЕ ВИДАЛЕННЯ (через сервер)
        const deleteBtn = itemDiv.querySelector('.delete-icon');
        deleteBtn.onclick = async (event) => {
            event.stopPropagation(); 
            
            if (confirm(`Видалити подію "${e.title}"?`)) {
                // Відправляємо запит на сервер для видалення
                const res = await fetch(`${API_URL}/${e.id}`, { 
                    method: 'DELETE', 
                    headers: getAuthHeaders() 
                });
                
                // Якщо сервер відповів "ок", оновлюємо календар
                if (res.ok) {
                    renderCalendar(); 
                }
            }
        };

        // 2. РОБОЧЕ РЕДАГУВАННЯ
        const editBtn = itemDiv.querySelector('.edit-icon');
        editBtn.onclick = (event) => {
            event.stopPropagation();
            
            // Викликаємо твою існуючу функцію відкриття вікна редагування
            openEditModal(e); 
        };

        listEl.appendChild(itemDiv);
    });
}

// --- ІНІЦІАЛІЗАЦІЯ ---

document.addEventListener('DOMContentLoaded', () => {
    initializeUserInfo(); // ДОДАНО: Викликаємо підстановку імені
    loadCategories();
    renderCalendar();

    document.getElementById('eventForm').addEventListener('submit', saveEvent);
    
    // ДОДАНО: Обробник для кнопки "Вихід"
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            logout();
        });
    }

    // ДОДАНО: Обробник для кнопки "Скасувати"
    const cancelBtn = document.getElementById('cancelBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', (e) => {
            e.preventDefault();
            closeModal();
        });
    }

    document.getElementById('recurrencePattern').addEventListener('change', (ev) => {
        const pattern = parseInt(ev.target.value);
        document.getElementById('recurrenceEndDateWrapper').style.display = (pattern !== 0 && pattern !== 4) ? 'block' : 'none';
    });

    document.getElementById('categoryId').addEventListener('change', (ev) => {
        document.getElementById('customCategoryWrapper').style.display = ev.target.value === 'other' ? 'block' : 'none';
    });

    document.getElementById('prevMonth').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    });
    document.getElementById('nextMonth').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    });

    window.onclick = (e) => {
        if (e.target == document.getElementById('eventModal')) {
            closeModal();
        }
    };
});