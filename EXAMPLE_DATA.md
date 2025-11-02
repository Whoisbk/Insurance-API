# Example Data for Creating Claims and Quotes

## 1. Creating a Claim

### Endpoint

`POST /api/claims/for-provider`

### Example JSON Request Body

```json
{
  "insurerId": 1,
  "providerId": 2,
  "title": "Water Damage Repair - Kitchen Flooring",
  "description": "Water leak from burst pipe caused significant damage to kitchen flooring and adjacent walls. Requires flooring replacement, wall repair, and potential mold remediation.",
  "priority": 2,
  "estimatedAmount": 8500.0,
  "policyNumber": "POL-2024-001234",
  "policyHolderName": "John Smith",
  "incidentLocation": "123 Main Street, Apt 4B, New York, NY 10001",
  "incidentDate": "2024-10-15T08:30:00Z",
  "dueDate": "2024-11-15T23:59:59Z",
  "notes": "Client reported incident on 10/15/2024. Initial inspection completed. Waiting for provider assessment.",
  "category": "Water Damage"
}
```

### Minimal Example (Required Fields Only)

```json
{
  "insurerId": 1,
  "providerId": 2,
  "title": "Roof Repair Needed",
  "estimatedAmount": 3500.0
}
```

### Field Descriptions

- **insurerId** (required): ID of the insurer/insurance company user (integer, > 0)
- **providerId** (required): ID of the service provider user (integer, > 0)
- **title** (required): Short title/name for the claim (max 255 characters)
- **description** (optional): Detailed description of the claim (max 2000 characters)
- **priority** (optional): Priority level - `1` = Low, `2` = Medium (default), `3` = High, `4` = Urgent
- **estimatedAmount** (required): Estimated cost of the claim (decimal, 0.01 to 999999999.99)
- **policyNumber** (optional): Insurance policy number (max 100 characters)
- **policyHolderName** (optional): Name of the policy holder (max 100 characters)
- **incidentLocation** (optional): Where the incident occurred (max 255 characters)
- **incidentDate** (optional): Date and time of the incident (ISO 8601 format)
- **dueDate** (optional): Deadline for claim completion (ISO 8601 format)
- **notes** (optional): Additional notes about the claim (max 1000 characters)
- **category** (optional): Category/type of claim (max 100 characters, e.g., "Water Damage", "Fire Damage", "Theft", etc.)

---

## 2. Creating a Quote

### Endpoint

`POST /api/quotes`

### Headers

```
X-User-Id: 2
Content-Type: application/json
```

### Example JSON Request Body (with Base64 Attachment)

```json
{
  "claimId": 10,
  "amount": 8750.0,
  "attachments": [
  ]
}
```

### Example JSON Request Body (with URL Attachment)

```json
{
  "claimId": 10,
  "amount": 8750.0,
  "attachments": [
  ]
}
```

### Minimal Example (No Attachments)

```json
{
  "claimId": 10,
  "amount": 8750.0
}
```

### Field Descriptions

#### Quote Fields

- **claimId** (required): ID of the claim this quote is for (integer, > 0)
- **amount** (required): Quote amount in dollars (decimal, 0.01 to 999999999.99)
- **attachments** (optional): Array of attachment objects

#### Attachment Fields

- **fileName** (required): Name of the file (max 255 characters)
- **contentBase64** (optional): Base64-encoded file content. Must provide if `url` and `storagePath` are not provided
- **url** (optional): Remote URL to the file. Must provide if `contentBase64` and `storagePath` are not provided
- **storagePath** (optional): Storage path if file is already stored. Must provide if `contentBase64` and `url` are not provided
- **mimeType** (optional): MIME type of the file (default: "application/octet-stream", max 100 characters)
- **documentType** (optional): Type of document:
  - `1` = DetailedEstimate
  - `2` = MaterialSpecification
  - `3` = LaborBreakdown
  - `4` = EquipmentRental
  - `5` = PermitFees
  - `6` = TimelineDocument
  - `7` = WarrantyDocument
  - `8` = TechnicalDrawings
  - `9` = Other (default)
- **title** (optional): Title for the document (max 255 characters)
- **description** (optional): Description of the document (max 500 characters)
- **tags** (optional): Comma-separated tags (max 255 characters)
- **size** (optional): File size in bytes (long integer)
- **uploadedAt** (optional): Upload timestamp (ISO 8601 format)

### Notes

1. For attachments, you must provide **at least one** of: `contentBase64`, `url`, or `storagePath`
2. The `claimId` must reference an existing claim in the database
3. The quote will be automatically associated with the provider from the claim
4. The `X-User-Id` header is optional but recommended for tracking who created the quote

---

## 3. Complete Workflow Example

### Step 1: Create a Claim

```json
POST /api/claims/for-provider
{
  "insurerId": 1,
  "providerId": 2,
  "title": "Roof Damage from Storm",
  "description": "Severe wind damage to roof shingles and gutters during last week's storm. Multiple sections need replacement.",
  "priority": 3,
  "estimatedAmount": 12000.00,
  "policyNumber": "POL-2024-005678",
  "policyHolderName": "Jane Doe",
  "incidentLocation": "456 Oak Avenue, Springfield, IL 62701",
  "incidentDate": "2024-10-20T14:00:00Z",
  "category": "Storm Damage"
}
```

### Step 2: Create a Quote for the Claim

```json
POST /api/quotes
Headers: X-User-Id: 2

{
  "claimId": 15,
  "amount": 11850.00,
  "attachments": [
    {
      "fileName": "roof-repair-estimate.pdf",
      "contentBase64": "BASE64_ENCODED_PDF_CONTENT_HERE",
      "mimeType": "application/pdf",
      "documentType": 1,
      "title": "Roof Repair Estimate",
      "description": "Detailed estimate for roof shingle replacement"
    }
  ]
}
```

---

## Priority Enum Values

For **Claim Priority**:

- `1` = Low
- `2` = Medium (default)
- `3` = High
- `4` = Urgent

## Quote Document Type Enum Values

For **Quote Document Type**:

- `1` = DetailedEstimate
- `2` = MaterialSpecification
- `3` = LaborBreakdown
- `4` = EquipmentRental
- `5` = PermitFees
- `6` = TimelineDocument
- `7` = WarrantyDocument
- `8` = TechnicalDrawings
- `9` = Other (default)
