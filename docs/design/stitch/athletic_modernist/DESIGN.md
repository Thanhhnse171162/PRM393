---
name: Athletic Modernist
colors:
  surface: '#faf8ff'
  surface-dim: '#d2d9f4'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f3ff'
  surface-container: '#eaedff'
  surface-container-high: '#e2e7ff'
  surface-container-highest: '#dae2fd'
  on-surface: '#131b2e'
  on-surface-variant: '#3e4947'
  inverse-surface: '#283044'
  inverse-on-surface: '#eef0ff'
  outline: '#6e7977'
  outline-variant: '#bdc9c6'
  surface-tint: '#006a63'
  primary: '#005c55'
  on-primary: '#ffffff'
  primary-container: '#0f766e'
  on-primary-container: '#a3faef'
  inverse-primary: '#80d5cb'
  secondary: '#006c49'
  on-secondary: '#ffffff'
  secondary-container: '#6cf8bb'
  on-secondary-container: '#00714d'
  tertiary: '#734700'
  on-tertiary: '#ffffff'
  tertiary-container: '#945d00'
  on-tertiary-container: '#ffe6cc'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#9cf2e8'
  primary-fixed-dim: '#80d5cb'
  on-primary-fixed: '#00201d'
  on-primary-fixed-variant: '#00504a'
  secondary-fixed: '#6ffbbe'
  secondary-fixed-dim: '#4edea3'
  on-secondary-fixed: '#002113'
  on-secondary-fixed-variant: '#005236'
  tertiary-fixed: '#ffddb8'
  tertiary-fixed-dim: '#ffb95f'
  on-tertiary-fixed: '#2a1700'
  on-tertiary-fixed-variant: '#653e00'
  background: '#faf8ff'
  on-background: '#131b2e'
  surface-variant: '#dae2fd'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.02em
  display-sm:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
    letterSpacing: -0.015em
  headline-lg:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.01em
  title-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 22px
    letterSpacing: -0.005em
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-lg:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 18px
    letterSpacing: 0.01em
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
    letterSpacing: 0.03em
  currency-display:
    fontFamily: Inter
    fontSize: 22px
    fontWeight: '700'
    lineHeight: 28px
    letterSpacing: -0.02em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 0.75rem
  gutter-md: 1rem
  margin: 1rem
  margin-md: 1.25rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
---

## Brand & Style

This design system drives a modern, high-velocity sports court reservation platform tailored for active urban communities in Vietnam. The experience communicates precision, agility, and effortless coordination. It bridges the kinetic energy of badminton, football, pickleball, and tennis with the frictionless utility of contemporary mobile product architecture.

### Emotional Response
The interface evokes readiness, clarity, and confidence. Users experience zero cognitive friction when scanning complex time-slot schedules, confirming multi-court sessions, or reviewing dynamic Vietnamese Dong (₫ / VND) pricing structures. It feels crisp like fresh synthetic turf, structured like court line markings, and dependable during high-demand booking windows.

### Visual Style
The system employs **Modern Athletic Minimalism**: high-contrast legibility, generous breathing room, crisp neutral layering, and intentional tactical accents. The aesthetic departs from cluttered tournament trackers by utilizing clean architectural surfaces, purposeful color-coded reservation states, and tactile mobile-first touch surfaces optimized for standard Flutter viewports.

## Colors

The palette is engineered around an athletic turf emerald hierarchy paired with high-clarity status signifiers and pristine neutral surfaces.

### Primary & Functional Roles
- **Primary Turf Teal (`#0F766E` / `#059669`):** Represents structural brand identity, active header states, primary navigational focus, and elevated interactive triggers.
- **Vibrant Emerald (`#10B981`):** Represents open court availability, successful bookings, confirmed payment receipts, and active live-slot highlights. Paired with soft tint `#D1FAE5` for surface backgrounds.
- **Sports Amber (`#F59E0B`):** Denotes temporal friction: reserved-in-cart timers, pending deposit verifications, peak-hour surcharges, and limited-slot alerts. Paired with container `#FEF3C7`.
- **Alert Crimson (`#EF4444`):** Flags cancellations, expired reservation holds, sold-out courts, and system errors. Paired with container `#FEE2E2`.

### Neutral & Surface Hierarchy
- **Canvas Base (`#F8FAFC`):** Primary scaffold background across mobile views.
- **Card & Sheet Surface (`#FFFFFF`):** Elevated cards, slot panels, modal bottom sheets, and sticky action docks.
- **Subtle Surface Container (`#F1F5F9`):** Input field fills, disabled slot indicators, segmented tab bars, and table header rows.
- **Primary Text (`#0F172A`):** Deep charcoal delivering maximum contrast against white cards for high-speed daylight scanning.
- **Secondary Text (`#334155`):** Subtext, court specifications, venue distances, and metadata labels.
- **Muted Supporting Text (`#64748B`):** Inactive slot times, past bookings, and assistive hints.
- **Borders & Dividers (`#E2E8F0`):** Hairline structure for slots, list divisions, and container outlines.

## Typography

The typographic hierarchy utilizes **Inter** throughout all tiers to guarantee high clarity across numeric density, Vietnamese diacritics (dấu tiếng Việt), and compact mobile layouts.

### Special Formatting Rules
- **Vietnamese Currency (VND):** Always rendered with `tabular-nums` formatting. For summary prices and sticky checkout buttons, format as `240.000 ₫` using `title-md` or `currency-display` with weight `700`.
- **Time Slots:** Slot matrix representations (e.g., `18:00 - 19:30`) use `label-md` with semi-bold weighting to maintain legibility inside compressed grid cells.
- **Diacritics Balance:** Line heights are maintained at a minimum factor of 1.35x across mobile body styles to ensure accented uppercase and lowercase characters (`ễ`, `ặ`, `ở`, `Đ`) avoid clipping.

## Layout & Spacing

The layout is optimized for high-performance touch devices with single-handed thumb operation.

### Grid & Canvas Structure
- **Mobile Grid (Flutter Scaffold):** 4-column dynamic grid with `16px` outer horizontal margins and `12px` gutters between items.
- **Tablet / Large Mobile Fold:** 8-column layout with `20px` margins and `16px` gutters.
- **Vertical Spacing Scale:** Built on a `4px` sub-grid, pacing screens primarily with `space-sm` (8px), `space-md` (16px), and `space-lg` (24px) gaps to ensure rhythmic flow between court carousels and filter chips.

### Mobile Safe Areas & Sticky Footers
- **Bottom Navigation Dock:** Fixed `64px` height exclusive of device home-indicator safe inset.
- **Sticky Booking CTA Bar:** Elevated container docked to the screen bottom with `16px` padding around primary actions, reserving `34px` bottom padding on notch-enabled mobile displays.

## Elevation & Depth

Visual hierarchy uses clean ambient layering rather than heavy skeuomorphic shading, preserving outdoor visibility under sunlight.

### Layering Levels
- **Level 0 (Flat Base):** `#F8FAFC` background canvas with zero elevation.
- **Level 1 (Card & Slot Rest):** Crisp white surface (`#FFFFFF`) framed by a subtle 1px border (`#E2E8F0`) with an ambient drop: `0px 1px 3px rgba(15, 23, 42, 0.04), 0px 4px 12px rgba(15, 23, 42, 0.03)`.
- **Level 2 (Active/Selected Card, Top AppBar Scrolled):** White surface with `0px 4px 16px rgba(15, 118, 110, 0.08), 0px 1px 2px rgba(0, 0, 0, 0.04)`.
- **Level 3 (Sticky Bottom CTA Bar & Action Sheets):** `#FFFFFF` with `0px -4px 20px rgba(15, 23, 42, 0.06)`, producing an upward separation from the scrollable screen content.
- **Level 4 (Modal Dialogs & Time Picker Overlays):** `#FFFFFF` with `0px 12px 32px rgba(15, 23, 42, 0.16)`.

## Shapes

The design uses balanced, modern curves that reflect athletic geometry and modern mobile platform conventions.

- **Cards & Venue Panels:** `rounded-lg` (16px) creates a friendly, organized layout for venue cards, schedule containers, and booking summaries.
- **Interactive Buttons:** `rounded-lg` (12px to 14px) provides solid target areas for full-width tap targets.
- **Chips, Badges & Status Indicators:** Fully circular / pill styling (`9999px`) for court filter tags (e.g., "Indoor", "AC", "Pickleball"), status flags ("Available", "Pending"), and calendar dates.
- **Slot Time Grids:** `rounded-md` (8px to 10px) delivers sharp, neat visual groupings inside high-density schedules.
- **Bottom Sheets:** Top-left and top-right radii set to `20px` with a centered `36px x 4px` grab handle.

## Components

### Buttons
- **Primary CTA:** Height `52px`, background `#0F766E`, foreground `#FFFFFF`, typography `label-lg`, radius `14px`. Hover/pressed state shifts to `#0D655E`. Supports leading icons and right-aligned total prices (e.g., `Tiếp tục • 320.000 ₫`).
- **Secondary / Ghost:** Height `48px`, background `transparent`, 1.5px border `#0F766E`, foreground `#0F766E`, radius `14px`.
- **Destructive:** Background `#FEE2E2`, foreground `#EF4444`, radius `14px`.

### Slot Selection Grid (Core Court Matrix)
A responsive multi-column grid indicating hourly court availability:
- **Available:** White surface `#FFFFFF`, 1px border `#CBD5E1`, text `#0F172A`. On tap, transitions to Selected.
- **Selected:** Solid primary teal `#0F766E`, text `#FFFFFF`, bold check icon or check tick.
- **Held / In Checkout (Timer):** Background `#FEF3C7`, border `#F59E0B`, text `#B45309`, showing subtle countdown indicator.
- **Unavailable / Booked:** Background `#F1F5F9`, border transparent, text `#94A3B8` with strikethrough or diagonal crossmark.

### Court & Venue Cards
- Background `#FFFFFF`, corner radius `16px`, padding `16px`.
- Features an aspect-ratio `16:9` image thumbnail with an absolute-positioned rating badge top-right (`#0F172A` at 80% opacity with star icon) and sport type tag bottom-left.
- Footer row splits metadata (distance in km, venue district) on the left, and standard hourly starting price (e.g., `từ 120.000 ₫/h`) in bold `#0F766E` on the right.

### Input Fields & Search Bars
- **Height:** `48px`, background `#F8FAFC`, border `1px solid #E2E8F0`, corner radius `12px`.
- **Focused State:** Background `#FFFFFF`, border `1.5px solid #0F766E`, ambient focus ring.
- **Leading/Trailing:** Search icon `#64748B` leading; clear button or filter adjustment icon trailing.

### Chips & Segmented Controls
- **Filter Chips:** Height `36px`, pill shape, padding `8px 14px`. Default state is `#F1F5F9` surface with `#334155` text. Active state is solid `#0F766E` with `#FFFFFF` text.
- **Segmented Control:** Enclosed container `#F1F5F9` with sliding white pill indicator `#FFFFFF` elevated at Level 1.

### Bottom Navigation Bar
- Flutter `BottomNavigationBar` configuration with 4 destinations: Khám phá (Explore), Đặt sân (My Bookings), Hoạt động (Activity), and Tài khoản (Profile).
- Inactive items: `#64748B`, active items: `#0F766E`. Active item includes a subtle 4px dot indicator underneath the icon.

### Modal Bottom Sheets
- Triggered for date-time pickers, court filter matrices, and payment breakdowns.
- Background `#FFFFFF`, rounded top corners `20px`, persistent drag pill at top center, followed by header with title and close icon.