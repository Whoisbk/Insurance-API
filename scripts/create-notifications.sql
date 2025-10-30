-- Create Additional Notifications in the Database
-- This script adds more notifications for testing purposes
-- Run with: mysql -h localhost -P 3306 -u root -p InsuranceClaimsDB < scripts/create-notifications.sql

USE InsuranceClaimsDB;

-- Check what users exist first
-- Assuming: User ID 2 = Insurer, User ID 3 = Provider

-- Insert Additional Notifications for Insurer (User ID 2)
INSERT INTO
    Notifications (
        UserId,
        QuoteId,
        Message,
        DateSent,
        Status
    )
SELECT
    2 AS UserId,
    q.QuoteId,
    CONCAT(
        'New quote submission received: $',
        FORMAT(q.Amount, 2),
        ' for claim ',
        c.ClaimNumber
    ) AS Message,
    NOW() AS DateSent,
    1 AS Status
FROM Quotes q
    INNER JOIN Claims c ON q.PolicyId = c.Id
WHERE
    q.Status = 1 -- Submitted status
    AND NOT EXISTS (
        SELECT 1
        FROM Notifications n
        WHERE
            n.QuoteId = q.QuoteId
            AND n.UserId = 2
            AND n.Message LIKE CONCAT(
                '%quote%',
                FORMAT(q.Amount, 2),
                '%'
            )
    )
LIMIT 5;

-- Insert Additional Notifications for Provider (User ID 3)
INSERT INTO
    Notifications (
        UserId,
        QuoteId,
        Message,
        DateSent,
        Status
    )
SELECT
    3 AS UserId,
    q.QuoteId,
    CASE
        WHEN q.Status = 2 THEN CONCAT(
            'Your quote for $',
            FORMAT(q.Amount, 2),
            ' has been approved for claim ',
            c.ClaimNumber
        )
        WHEN q.Status = 3 THEN CONCAT(
            'Your quote for $',
            FORMAT(q.Amount, 2),
            ' has been rejected for claim ',
            c.ClaimNumber
        )
        WHEN q.Status = 4 THEN CONCAT(
            'Your quote for $',
            FORMAT(q.Amount, 2),
            ' requires revision for claim ',
            c.ClaimNumber
        )
        ELSE CONCAT(
            'Your quote for $',
            FORMAT(q.Amount, 2),
            ' status updated for claim ',
            c.ClaimNumber
        )
    END AS Message,
    DATE_SUB(
        NOW(),
        INTERVAL FLOOR(RAND() * 7) DAY
    ) AS DateSent,
    CASE
        WHEN RAND() > 0.5 THEN 1
        ELSE 2
    END AS Status
FROM Quotes q
    INNER JOIN Claims c ON q.PolicyId = c.Id
WHERE
    q.Status IN (2, 3, 4) -- Approved, Rejected, or Revised
    AND NOT EXISTS (
        SELECT 1
        FROM Notifications n
        WHERE
            n.QuoteId = q.QuoteId
            AND n.UserId = 3
            AND n.Message LIKE CONCAT(
                '%quote%',
                FORMAT(q.Amount, 2),
                '%'
            )
    )
LIMIT 10;

-- Create notifications for new claims assigned to provider
INSERT INTO
    Notifications (
        UserId,
        QuoteId,
        Message,
        DateSent,
        Status
    )
SELECT
    3 AS UserId,
    NULL AS QuoteId,
    CONCAT(
        'New claim ',
        c.ClaimNumber,
        ' has been assigned to you. Priority: ',
        CASE c.Priority
            WHEN 1 THEN 'Low'
            WHEN 2 THEN 'Medium'
            WHEN 3 THEN 'High'
            WHEN 4 THEN 'Urgent'
            ELSE 'Normal'
        END
    ) AS Message,
    c.CreatedAt AS DateSent,
    1 AS Status
FROM Claims c
WHERE
    c.ProviderId = 3
    AND c.CreatedAt > DATE_SUB(NOW(), INTERVAL 30 DAY)
    AND NOT EXISTS (
        SELECT 1
        FROM Notifications n
        WHERE
            n.UserId = 3
            AND n.QuoteId IS NULL
            AND n.Message LIKE CONCAT('%claim ', c.ClaimNumber, '%')
    )
LIMIT 5;

-- Create general notifications for provider
INSERT INTO
    Notifications (
        UserId,
        QuoteId,
        Message,
        DateSent,
        Status
    )
VALUES (
        3,
        NULL,
        'Reminder: You have 3 pending quotes that require attention',
        DATE_SUB(NOW(), INTERVAL 1 DAY),
        1
    ),
    (
        3,
        NULL,
        'Weekly summary: 5 quotes submitted this week',
        DATE_SUB(NOW(), INTERVAL 2 DAY),
        2
    ),
    (
        3,
        NULL,
        'Claim CLM-2024-004 deadline approaching - quote submission required',
        NOW(),
        1
    ),
    (
        3,
        NULL,
        'System maintenance scheduled for tonight at 2 AM',
        DATE_SUB(NOW(), INTERVAL 3 HOUR),
        1
    );

-- Create general notifications for insurer
INSERT INTO
    Notifications (
        UserId,
        QuoteId,
        Message,
        DateSent,
        Status
    )
VALUES (
        2,
        NULL,
        'You have 5 pending quotes awaiting review',
        NOW(),
        1
    ),
    (
        2,
        NULL,
        'Claim CLM-2024-002 requires immediate attention - Urgent priority',
        DATE_SUB(NOW(), INTERVAL 1 HOUR),
        1
    ),
    (
        2,
        NULL,
        'Weekly report: 12 quotes reviewed this week',
        DATE_SUB(NOW(), INTERVAL 1 DAY),
        2
    ),
    (
        2,
        NULL,
        'New provider registered - ABC Construction Services',
        DATE_SUB(NOW(), INTERVAL 4 HOUR),
        1
    );

-- Display summary
SELECT 'Additional notifications created!' AS Status;

SELECT
    UserId,
    CASE UserId
        WHEN 2 THEN 'Insurer'
        WHEN 3 THEN 'Provider'
        ELSE CONCAT('User ', UserId)
    END AS UserType,
    Status,
    CASE Status
        WHEN 1 THEN 'Unread'
        WHEN 2 THEN 'Read'
    END AS StatusName,
    COUNT(*) AS Count
FROM Notifications
GROUP BY
    UserId,
    Status
ORDER BY UserId, Status;

SELECT
    'Total Notifications' AS Summary,
    COUNT(*) AS TotalCount,
    SUM(
        CASE
            WHEN Status = 1 THEN 1
            ELSE 0
        END
    ) AS UnreadCount,
    SUM(
        CASE
            WHEN Status = 2 THEN 1
            ELSE 0
        END
    ) AS ReadCount
FROM Notifications;