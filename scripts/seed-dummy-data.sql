-- Dummy/Mock Data for Claims and Notifications
-- This script inserts sample claims and notifications for testing purposes
-- Run with: mysql -h localhost -P 3306 -u root -p InsuranceClaimsDB < scripts/seed-dummy-data.sql

USE InsuranceClaimsDB;

-- Note: This script assumes you have at least 3 users:
-- User ID 1: Admin
-- User ID 2: Insurer
-- User ID 3: Provider
-- If your user IDs are different, adjust the ProviderId and InsurerId values below

-- Clean up existing dummy data (optional - comment out if you want to keep existing data)
-- Delete notifications first (foreign key constraint) - set QuoteId to NULL first
UPDATE Notifications
SET
    QuoteId = NULL
WHERE
    Message LIKE '%CLM-2024-%';

DELETE FROM Notifications WHERE Message LIKE '%CLM-2024-%';
-- Delete quotes that reference claims we're deleting
DELETE q
FROM Quotes q
    INNER JOIN Claims c ON q.PolicyId = c.Id
WHERE
    c.ClaimNumber LIKE 'CLM-2024-%';
-- Delete claims
DELETE FROM Claims WHERE ClaimNumber LIKE 'CLM-2024-%';

-- Insert Dummy Claims
-- ClaimStatus: Draft=1, Submitted=2, UnderReview=3, Approved=4, Rejected=5, Closed=6
-- ClaimPriority: Low=1, Medium=2, High=3, Urgent=4

INSERT INTO
    Claims (
        ClaimNumber,
        Title,
        Description,
        ProviderId,
        InsurerId,
        Status,
        Priority,
        EstimatedAmount,
        ApprovedAmount,
        PolicyNumber,
        PolicyHolderName,
        IncidentLocation,
        IncidentDate,
        ClaimSubmittedDate,
        DueDate,
        Notes,
        Category,
        CreatedAt,
        UpdatedAt
    )
VALUES
    -- Claim 1: Water Damage (High Priority, Under Review)
    (
        'CLM-2024-001',
        'Water Damage - Basement Flooding',
        'Basement experienced significant flooding due to broken water pipe. Water damage to flooring, drywall, and furniture. Immediate action required to prevent mold growth.',
        3, -- ProviderId (assuming Provider user is ID 3)
        2, -- InsurerId (assuming Insurer user is ID 2)
        3, -- Status: UnderReview
        3, -- Priority: High
        15000.00,
        0.00,
        'POL-2023-5001',
        'John Smith',
        '123 Main Street, Anytown, ST 12345',
        '2024-01-15 14:30:00',
        '2024-01-16 09:00:00',
        '2024-02-15 17:00:00',
        'Customer reported incident immediately. Adjuster has been dispatched for assessment.',
        'Water Damage',
        '2024-01-16 09:00:00',
        '2024-01-20 10:30:00'
    ),

-- Claim 2: Fire Damage (Urgent Priority, Submitted)
(
    'CLM-2024-002',
    'Kitchen Fire Damage',
    'Kitchen fire caused by electrical malfunction. Significant damage to cabinets, appliances, and surrounding areas. Smoke damage throughout first floor.',
    3, -- ProviderId
    2, -- InsurerId
    2, -- Status: Submitted
    4, -- Priority: Urgent
    35000.00,
    0.00,
    'POL-2023-5002',
    'Jane Doe',
    '456 Oak Avenue, Springville, ST 54321',
    '2024-01-18 22:15:00',
    '2024-01-19 08:30:00',
    '2024-02-18 17:00:00',
    'Fire department responded promptly. Structure is safe but requires extensive repairs.',
    'Fire Damage',
    '2024-01-19 08:30:00',
    '2024-01-19 08:30:00'
),

-- Claim 3: Roof Damage (Medium Priority, Approved)
(
    'CLM-2024-003',
    'Roof Damage from Hailstorm',
    'Severe hail damage to roof shingles and gutters. Multiple areas need replacement. Minor water leakage into attic.',
    3, -- ProviderId
    2, -- InsurerId
    4, -- Status: Approved
    2, -- Priority: Medium
    8500.00,
    8200.00,
    'POL-2023-5003',
    'Robert Johnson',
    '789 Pine Road, Hilltown, ST 67890',
    '2024-01-10 16:00:00',
    '2024-01-11 10:00:00',
    '2024-02-10 17:00:00',
    'Hail storm occurred during severe weather warning. Damage verified by adjuster.',
    'Property Damage',
    '2024-01-11 10:00:00',
    '2024-01-25 14:20:00'
),

-- Claim 4: Wind Damage (High Priority, Draft)
(
    'CLM-2024-004',
    'Wind Damage - Siding and Windows',
    'High winds caused damage to exterior siding and several windows. Fence also damaged. Temporary repairs completed.',
    3, -- ProviderId
    2, -- InsurerId
    1, -- Status: Draft
    3, -- Priority: High
    12000.00,
    0.00,
    'POL-2023-5004',
    'Maria Garcia',
    '321 Elm Street, Rivertown, ST 11223',
    '2024-01-22 13:45:00',
    NULL, -- Not yet submitted
    '2024-02-22 17:00:00',
    'Claim being prepared with documentation and photos.',
    'Wind Damage',
    '2024-01-22 15:00:00',
    '2024-01-22 15:00:00'
),

-- Claim 5: Vandalism (Low Priority, Closed)
(
    'CLM-2024-005',
    'Vandalism - Graffiti and Property Damage',
    'Property vandalized with graffiti on exterior walls. Broken windows and damaged landscaping. Police report filed.',
    3, -- ProviderId
    2, -- InsurerId
    6, -- Status: Closed
    1, -- Priority: Low
    3200.00,
    3000.00,
    'POL-2023-5005',
    'David Lee',
    '654 Maple Drive, Laketown, ST 44556',
    '2024-01-05 08:00:00',
    '2024-01-05 11:00:00',
    '2024-02-05 17:00:00',
    'Claim completed and payment processed. All repairs completed.',
    'Vandalism',
    '2024-01-05 11:00:00',
    '2024-01-30 16:00:00'
),

-- Claim 6: Theft (Medium Priority, Rejected)
(
    'CLM-2024-006',
    'Theft - Stolen Electronics',
    'Burglary occurred resulting in theft of electronics and jewelry. Police report number: PR-2024-1234.',
    3, -- ProviderId
    2, -- InsurerId
    5, -- Status: Rejected
    2, -- Priority: Medium
    5500.00,
    0.00,
    'POL-2023-5006',
    'Sarah Williams',
    '987 Cedar Lane, Mountview, ST 77889',
    '2024-01-12 19:30:00',
    '2024-01-13 09:00:00',
    '2024-02-12 17:00:00',
    'Claim rejected due to insufficient coverage for items claimed. Appeal in process.',
    'Theft',
    '2024-01-13 09:00:00',
    '2024-01-28 11:45:00'
);

-- Insert Dummy Quotes (assuming we have claims above)
-- QuoteStatus: Submitted=1, Approved=2, Rejected=3, Revised=4
-- Note: PolicyId in Quotes table refers to ClaimId
-- We'll use subqueries to get the actual Claim IDs based on ClaimNumber

INSERT INTO
    Quotes (
        PolicyId,
        ProviderId,
        Amount,
        Status,
        DateSubmitted
    )
VALUES
    -- Quote for Claim CLM-2024-001 (Water Damage)
    (
        (
            SELECT Id
            FROM Claims
            WHERE
                ClaimNumber = 'CLM-2024-001'
            LIMIT 1
        ),
        3,
        14500.00,
        1,
        '2024-01-17 10:00:00'
    ), -- Submitted
    (
        (
            SELECT Id
            FROM Claims
            WHERE
                ClaimNumber = 'CLM-2024-001'
            LIMIT 1
        ),
        3,
        14800.00,
        1,
        '2024-01-18 14:30:00'
    ), -- Submitted (revised)

-- Quote for Claim CLM-2024-002 (Fire Damage)
(
    (
        SELECT Id
        FROM Claims
        WHERE
            ClaimNumber = 'CLM-2024-002'
        LIMIT 1
    ),
    3,
    34500.00,
    1,
    '2024-01-20 09:15:00'
), -- Submitted
(
    (
        SELECT Id
        FROM Claims
        WHERE
            ClaimNumber = 'CLM-2024-002'
        LIMIT 1
    ),
    3,
    35000.00,
    2,
    '2024-01-21 11:00:00'
), -- Approved

-- Quote for Claim CLM-2024-003 (Roof Damage)
(
    (
        SELECT Id
        FROM Claims
        WHERE
            ClaimNumber = 'CLM-2024-003'
        LIMIT 1
    ),
    3,
    8200.00,
    2,
    '2024-01-12 08:00:00'
), -- Approved

-- Quote for Claim CLM-2024-004 (Wind Damage)
(
    (
        SELECT Id
        FROM Claims
        WHERE
            ClaimNumber = 'CLM-2024-004'
        LIMIT 1
    ),
    3,
    11800.00,
    1,
    '2024-01-23 13:00:00'
), -- Submitted

-- Quote for Claim CLM-2024-005 (Vandalism)
(
    (
        SELECT Id
        FROM Claims
        WHERE
            ClaimNumber = 'CLM-2024-005'
        LIMIT 1
    ),
    3,
    3000.00,
    2,
    '2024-01-06 10:00:00'
);
-- Approved

-- Insert Dummy Notifications
-- NotificationStatus: Unread=1, Read=2
-- UserId: 2 = Insurer, 3 = Provider
-- Note: We'll get QuoteId from the Quotes table using subqueries to ensure foreign key references are valid

INSERT INTO
    Notifications (
        UserId,
        QuoteId,
        Message,
        DateSent,
        Status
    )
VALUES
    -- Notifications for Insurer (User ID 2) - New quotes submitted
    -- Get quote IDs by matching ClaimNumber and Amount
    (
        2,
        (
            SELECT q.QuoteId
            FROM Quotes q
                INNER JOIN Claims c ON q.PolicyId = c.Id
            WHERE
                c.ClaimNumber = 'CLM-2024-001'
                AND q.Amount = 14500.00
            LIMIT 1
        ),
        'A new quote for $14,500.00 has been submitted for claim CLM-2024-001',
        '2024-01-17 10:00:00',
        1
    ), -- Unread
    (
        2,
        (
            SELECT q.QuoteId
            FROM Quotes q
                INNER JOIN Claims c ON q.PolicyId = c.Id
            WHERE
                c.ClaimNumber = 'CLM-2024-001'
                AND q.Amount = 14800.00
            LIMIT 1
        ),
        'A new quote for $14,800.00 has been submitted for claim CLM-2024-001',
        '2024-01-18 14:30:00',
        1
    ), -- Unread
    (
        2,
        (
            SELECT q.QuoteId
            FROM Quotes q
                INNER JOIN Claims c ON q.PolicyId = c.Id
            WHERE
                c.ClaimNumber = 'CLM-2024-002'
                AND q.Amount = 34500.00
            LIMIT 1
        ),
        'A new quote for $34,500.00 has been submitted for claim CLM-2024-002',
        '2024-01-20 09:15:00',
        1
    ), -- Unread
    (
        2,
        (
            SELECT q.QuoteId
            FROM Quotes q
                INNER JOIN Claims c ON q.PolicyId = c.Id
            WHERE
                c.ClaimNumber = 'CLM-2024-002'
                AND q.Amount = 35000.00
            LIMIT 1
        ),
        'A new quote for $35,000.00 has been submitted for claim CLM-2024-002',
        '2024-01-21 11:00:00',
        2
    ), -- Read
    (
        2,
        (
            SELECT q.QuoteId
            FROM Quotes q
                INNER JOIN Claims c ON q.PolicyId = c.Id
            WHERE
                c.ClaimNumber = 'CLM-2024-003'
                AND q.Amount = 8200.00
            LIMIT 1
        ),
        'A new quote for $8,200.00 has been submitted for claim CLM-2024-003',
        '2024-01-12 08:00:00',
        2
    ), -- Read
    (
        2,
        (
            SELECT q.QuoteId
            FROM Quotes q
                INNER JOIN Claims c ON q.PolicyId = c.Id
            WHERE
                c.ClaimNumber = 'CLM-2024-004'
                AND q.Amount = 11800.00
            LIMIT 1
        ),
        'A new quote for $11,800.00 has been submitted for claim CLM-2024-004',
        '2024-01-23 13:00:00',
        1
    ), -- Unread

-- Notifications for Provider (User ID 3) - Quote status updates
(
    3,
    (
        SELECT q.QuoteId
        FROM Quotes q
            INNER JOIN Claims c ON q.PolicyId = c.Id
        WHERE
            c.ClaimNumber = 'CLM-2024-002'
            AND q.Amount = 35000.00
        LIMIT 1
    ),
    'Your quote for $35,000.00 has been approved',
    '2024-01-21 11:30:00',
    1
), -- Unread
(
    3,
    (
        SELECT q.QuoteId
        FROM Quotes q
            INNER JOIN Claims c ON q.PolicyId = c.Id
        WHERE
            c.ClaimNumber = 'CLM-2024-003'
            AND q.Amount = 8200.00
        LIMIT 1
    ),
    'Your quote for $8,200.00 has been approved: Damage assessment verified, proceed with repairs.',
    '2024-01-13 10:00:00',
    2
), -- Read
(
    3,
    (
        SELECT q.QuoteId
        FROM Quotes q
            INNER JOIN Claims c ON q.PolicyId = c.Id
        WHERE
            c.ClaimNumber = 'CLM-2024-005'
            AND q.Amount = 3000.00
        LIMIT 1
    ),
    'Your quote for $3,000.00 has been approved',
    '2024-01-07 10:00:00',
    2
), -- Read
(
    3,
    (
        SELECT q.QuoteId
        FROM Quotes q
            INNER JOIN Claims c ON q.PolicyId = c.Id
        WHERE
            c.ClaimNumber = 'CLM-2024-001'
            AND q.Amount = 14800.00
        LIMIT 1
    ),
    'Your quote for $14,800.00 requires revision: Please provide more detailed breakdown of labor and materials.',
    '2024-01-19 09:00:00',
    1
), -- Unread
(
    3,
    (
        SELECT q.QuoteId
        FROM Quotes q
            INNER JOIN Claims c ON q.PolicyId = c.Id
        WHERE
            c.ClaimNumber = 'CLM-2024-001'
            AND q.Amount = 14500.00
        LIMIT 1
    ),
    'Your quote for $14,500.00 has been rejected: Quote exceeds estimated amount without justification.',
    '2024-01-18 11:00:00',
    2
), -- Read

-- Additional notifications for Provider (no quote reference)
(
    3,
    NULL,
    'New claim CLM-2024-006 requires your attention',
    '2024-01-13 09:00:00',
    1
), -- Unread
(
    3,
    NULL,
    'Reminder: Claim CLM-2024-004 is pending quote submission',
    '2024-01-24 09:00:00',
    1
), -- Unread
(
    3,
    (
        SELECT q.QuoteId
        FROM Quotes q
            INNER JOIN Claims c ON q.PolicyId = c.Id
        WHERE
            c.ClaimNumber = 'CLM-2024-004'
            AND q.Amount = 11800.00
        LIMIT 1
    ),
    'Your quote for $11,800.00 has been updated',
    '2024-01-23 13:30:00',
    1
);
-- Unread

-- Display summary
SELECT 'Dummy data inserted successfully!' AS Status;

SELECT COUNT(*) AS TotalClaims FROM Claims;

SELECT COUNT(*) AS TotalQuotes FROM Quotes;

SELECT COUNT(*) AS TotalNotifications FROM Notifications;

SELECT Status, COUNT(*) AS Count
FROM Notifications
GROUP BY
    Status;