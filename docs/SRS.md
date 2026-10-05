# Software Requirements Specification (SRS) – CourtGo

## 1. Introduction
**Purpose:** mobile application for booking sports courts across CourtGo branches.
**Scope:** one Flutter mobile app (Customer, Staff, Admin) + ASP.NET Core Web API + SQL Server.
**Course:** PRM393 – 5-week project.

## 2. Actors
Guest, Customer, Staff (assigned to one branch), Admin.

## 3. Functional requirements

### 3.1 Customer / Guest
- FR-C1 Browse without login; choose search area
- FR-C2 Browse sport centers; filter by sport; view center details and courts
- FR-C3 Select date and fixed 1-hour slots (multiple consecutive)
- FR-C4 Create booking (login required)
- FR-C5 Pay deposit
- FR-C6 Booking history and booking QR
- FR-C7 Notifications
- FR-C8 Account management

### 3.2 Staff
- FR-S1 Operational dashboard; today's bookings; search/filter
- FR-S2 Scan QR and check in customer
- FR-S3 Collect remaining payment
- FR-S4 Update court operational status
- FR-S5 Create walk-in booking; complete booking
- FR-S6 Operational notifications

### 3.3 Admin
- FR-A1 Manage branches, courts, sports
- FR-A2 Manage staff accounts and branch assignment
- FR-A3 Manage opening hours and prices
- FR-A4 View all bookings; process cancellation/refund
- FR-A5 Simple reports; basic system configuration

## 4. Non-functional requirements
- Mobile-first UI (SafeArea, keyboard-safe, small screens, ≥44px touch targets)
- Loading / error / empty / success states on every screen
- JWT authentication; passwords hashed; no secrets in Git
- Prevent double booking (see business-rules.md BR-3)
- Vietnamese UI text; English code

## 5. Out of scope (initial)
Online payment gateway integration, full reporting analytics, push infrastructure.

## 6. Status
Initial scaffolding complete; features implemented one by one (see README Git workflow).
