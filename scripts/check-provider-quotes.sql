-- Check if there are quotes for a specific provider Firebase UID
-- Usage: Replace 'weW5agxPcPM1vHVSorJubkRciKo2' with the actual Firebase UID

USE InsuranceClaimsDB;

-- First, check if the provider exists
SELECT 
    u.Id,
    u.FirstName,
    u.LastName,
    u.Email,
    u.FirebaseUid,
    u.Role,
    u.Status,
    u.CompanyName
FROM Users u
WHERE u.FirebaseUid = 'weW5agxPcPM1vHVSorJubkRciKo2'
  AND u.Role = 2; -- Role 2 = Provider

-- Then, check if this provider has any quotes
SELECT 
    q.QuoteId,
    q.PolicyId,
    q.ProviderId,
    q.Amount,
    q.Status,
    q.DateSubmitted,
    u.FirstName AS ProviderFirstName,
    u.LastName AS ProviderLastName,
    u.Email AS ProviderEmail,
    u.FirebaseUid,
    COUNT(qd.Id) AS DocumentCount
FROM Quotes q
INNER JOIN Users u ON q.ProviderId = u.Id
LEFT JOIN QuoteDocuments qd ON q.QuoteId = qd.QuoteId
WHERE u.FirebaseUid = 'weW5agxPcPM1vHVSorJubkRciKo2'
  AND u.Role = 2
GROUP BY q.QuoteId, q.PolicyId, q.ProviderId, q.Amount, q.Status, q.DateSubmitted, 
         u.FirstName, u.LastName, u.Email, u.FirebaseUid
ORDER BY q.DateSubmitted DESC;

-- Summary count
SELECT 
    COUNT(*) AS TotalQuotes,
    u.FirebaseUid,
    u.FirstName,
    u.LastName
FROM Quotes q
INNER JOIN Users u ON q.ProviderId = u.Id
WHERE u.FirebaseUid = 'weW5agxPcPM1vHVSorJubkRciKo2'
  AND u.Role = 2
GROUP BY u.FirebaseUid, u.FirstName, u.LastName;



