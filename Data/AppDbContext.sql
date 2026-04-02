-- Створення нової бази даних з назвою TimePlannerDB
CREATE DATABASE TimePlannerDB;
USE TimePlannerDB;

-- ТАБЛИЦЯ КОРИСТУВАЧІВ (Users)
CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY, -- Унікальний ідентифікатор (автоматично зростає)
    Email VARCHAR(255) NOT NULL UNIQUE, -- Пошта для входу (не може повторюватись)
    PasswordHash VARCHAR(255) NOT NULL, -- Захешований пароль (не відкритий текст!)
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP, -- Дата реєстрації (ставиться автоматично)

    INDEX idx_user_email (Email)
);

-- ТАБЛИЦЯ КАТЕГОРІЙ (Categories)
CREATE TABLE Categories (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL, 
    ColorHex VARCHAR(7) NOT NULL, 

    -- Перевірка (Constraint): гарантує, що колір введений правильно (починається з # і має 6 символів)
    CONSTRAINT chk_color_format
        CHECK (ColorHex REGEXP '^#[0-9A-Fa-f]{6}$')
);

-- ТАБЛИЦЯ ПОДІЙ (Events) - основна таблиця календаря
CREATE TABLE Events (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,               -- Зовнішній ключ: до якого користувача належить подія
    Title VARCHAR(100) NOT NULL,       -- Назва події
    Description TEXT,                  -- Опис (може бути довгим)
    StartTime DATETIME NOT NULL,       -- Час початку
    EndTime DATETIME NOT NULL,         -- Час завершення
    CategoryId INT,                    -- Зовнішній ключ: категорія події (може бути порожнім)
    IsRecurring BOOLEAN DEFAULT FALSE, -- Чи подія повторювана
    RepeatType ENUM('none','daily','weekly','monthly') DEFAULT 'none', -- Тип повторення
    IsDeleted BOOLEAN DEFAULT FALSE, -- Прапорець для "м'якого видалення"
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, -- Оновлюється автоматично при зміні даних

    -- Перевірка (Constraint): час завершення не може бути раніше часу початку
    CONSTRAINT chk_event_time
        CHECK (EndTime > StartTime),

    -- Зв'язок з таблицею користувачів: якщо видалити користувача, видаляться і всі його події (CASCADE)
    CONSTRAINT fk_event_user
        FOREIGN KEY (UserId) REFERENCES Users(Id)
        ON DELETE CASCADE,

    -- Зв'язок з категоріями: якщо видалити категорію, у події вона просто стане NULL
    CONSTRAINT fk_event_category
        FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        ON DELETE SET NULL,
        
    -- Індекси для швидкого вибору подій конкретного юзера та фільтрації по часу
    INDEX idx_event_user (UserId),
    INDEX idx_event_time (StartTime, EndTime)
);

-- ТАБЛИЦЯ НАГАДУВАНЬ (Reminders)
CREATE TABLE Reminders (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventId INT NOT NULL,              -- До якої події відноситься нагадування

    RemindAt DATETIME NOT NULL,        -- Коли саме треба відправити сповіщення
    IsSent BOOLEAN DEFAULT FALSE,      -- Статус: чи було вже відправлено повідомлення

    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,

    -- Зв'язок з подіями: видалення події видаляє і її нагадування
    CONSTRAINT fk_reminder_event
        FOREIGN KEY (EventId) REFERENCES Events(Id)
        ON DELETE CASCADE,
    -- Індекс для фонової служби, яка постійно шукає нагадування за часом
    INDEX idx_reminder_time (RemindAt)
);