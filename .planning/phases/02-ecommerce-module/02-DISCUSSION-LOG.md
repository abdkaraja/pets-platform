# Phase 2: E-Commerce Module - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-19
**Phase:** 2-E-Commerce Module
**Areas discussed:** Product Variants, Payment Provider, Cart Persistence, Order Status Flow

---

## Product Variants

### Should products have variants?

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, variants needed | A 'Dog Food' product has 5kg, 10kg, 20kg variants with different prices/stock. Requires ProductVariant entity. | ✓ |
| No, each variant = separate product | Simpler model — 'Dog Food 5kg' and 'Dog Food 10kg' are separate Product entries. Easier admin but more products to manage. | |
| You decide | Let the architect decide based on domain needs. | |

**User's choice:** Yes, variants needed
**Notes:** User explicitly chose variants for realistic e-commerce experience

### Which variant attributes should products support?

| Option | Description | Selected |
|--------|-------------|----------|
| Size + Color | Covers most pet products — food bags (5kg/10kg), toys (S/M/L), accessories (Red/Blue). | |
| Size + Color + Weight | More precise — food by weight, supplements by volume, etc. | ✓ |
| Flexible key-value | Admin defines custom attributes per category (most flexible, more complex). | |

**User's choice:** Size + Color + Weight
**Notes:** User wants comprehensive variant support

### How should variant pricing work?

| Option | Description | Selected |
|--------|-------------|----------|
| Each variant has its own price | 5kg = $29.99, 10kg = $49.99, 20kg = $89.99. Most flexible, standard e-commerce approach. | |
| Base price + multiplier | Product has base price, variants apply a multiplier. Simpler but less flexible. | ✓ |

**User's choice:** Base price + multiplier
**Notes:** User prefers simpler pricing model

### Should each variant have its own image?

| Option | Description | Selected |
|--------|-------------|----------|
| Product-level image only | Simpler — one image per product, variants don't have separate photos. | ✓ |
| Each variant can have its own image | More realistic — Red collar looks different from Blue collar. Adds image management complexity. | |

**User's choice:** Product-level image only
**Notes:** User prioritizes simplicity over visual accuracy

### Should inventory be tracked per variant or per product?

| Option | Description | Selected |
|--------|-------------|----------|
| Per variant | 5kg has 10 in stock, 10kg has 5 in stock. More accurate, required if variants have different stock levels. | ✓ |
| Per product | Simpler — total stock across all variants. Less accurate but easier to manage. | |

**User's choice:** Per variant
**Notes:** User wants accurate inventory tracking

### Should variants have auto-generated SKUs or admin-assigned?

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-generated | System generates SKU from product code + variant attributes (e.g., DF-5KG-BLU). Less admin burden. | |
| Admin-assigned | Admin enters SKU manually. More control but more work. | ✓ |

**User's choice:** Admin-assigned
**Notes:** User wants manual control over SKUs

---

## Payment Provider

### Which payment integration approach for checkout?

| Option | Description | Selected |
|--------|-------------|----------|
| Stripe (Recommended) | Most popular, excellent .NET SDK (Stripe.net), handles cards + Apple Pay + Google Pay. Real integration from day one. | ✓ |
| PayPal | Widely recognized, good for international users. SDK available but less developer-friendly than Stripe. | |
| Mock/Stub for v1 | Simulate payment flow without real money. Faster to build, can integrate real payments later. | |

**User's choice:** Stripe (Recommended)
**Notes:** User chose real payment integration from day one

### How should Stripe checkout work?

| Option | Description | Selected |
|--------|-------------|----------|
| Stripe Checkout (hosted) | Redirect to Stripe's hosted checkout page. Simplest integration, Stripe handles PCI compliance. Less UI control. | |
| Stripe Elements (embedded) | Card input form embedded in your checkout page. More control over UX, still PCI-compliant. More work to build. | ✓ |
| You decide | Let the architect choose based on complexity tradeoffs. | |

**User's choice:** Stripe Elements (embedded)
**Notes:** User wants control over checkout UX

### How should payment confirmation work after Stripe processes payment?

| Option | Description | Selected |
|--------|-------------|----------|
| Webhook + redirect | Stripe sends webhook to confirm payment, user redirected to order confirmation page. Most reliable. | ✓ |
| Client-side only | Payment confirmed via Stripe.js response, immediate redirect. Simpler but less robust if user closes tab. | |

**User's choice:** Webhook + redirect
**Notes:** User prioritizes reliability

### Should we include Stripe test mode configuration from the start?

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, test mode ready | Include test API keys in appsettings.Development.json, test card numbers work out of the box. | ✓ |
| No, production only | Skip test config, user adds real keys when ready. | |

**User's choice:** Yes, test mode ready
**Notes:** User wants development-ready setup

---

## Cart Persistence

### How should the shopping cart persist across sessions?

| Option | Description | Selected |
|--------|-------------|----------|
| Database (per-user) (Recommended) | Cart stored in database, linked to user account. Survives logout, works across devices. Requires login to use cart. | ✓ |
| Client-side only | Cart stored in browser localStorage/cookie. Works without login, but lost on cache clear or different device. | |
| Hybrid (session + DB merge) | Merge both — anonymous cart in session, merge into DB cart on login. Most flexible but more complex. | |

**User's choice:** Database (per-user)
**Notes:** User wants persistent cart across sessions

### What happens when a user adds an out-of-stock item to cart?

| Option | Description | Selected |
|--------|-------------|----------|
| Block add (strict) | Block the add, show 'Out of stock' message immediately. | ✓ |
| Allow add, block at checkout | Allow adding, show warning, block at checkout if still out of stock. | |
| Allow add, warn only | Allow adding, show warning, but don't block — let admin decide fulfillment. | |

**User's choice:** Block add (strict)
**Notes:** User wants strict inventory enforcement

### Should there be a maximum quantity per item in the cart?

| Option | Description | Selected |
|--------|-------------|----------|
| Cap at stock level (Recommended) | Limit to available stock (can't add more than what's in inventory). | ✓ |
| No limit, validate at checkout | Allow any quantity, validate only at checkout. | |
| Admin-configurable limit | Admin sets max per product. | |

**User's choice:** Cap at stock level
**Notes:** User wants inventory-aware cart limits

### How should the cart handle price changes after items are added?

| Option | Description | Selected |
|--------|-------------|----------|
| Lock price at add time (Recommended) | Lock price at time of adding. User sees the price they agreed to. Standard e-commerce practice. | ✓ |
| Show current price at checkout | Show current price at checkout. User always sees latest price but may be surprised. | |

**User's choice:** Lock price at add time
**Notes:** User wants price transparency

### Should the cart auto-empty after successful checkout?

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, clear after checkout (Recommended) | Yes, clear cart after order is placed. Standard behavior. | ✓ |
| Keep items for reorder | Keep items in cart in case user wants to reorder. | |

**User's choice:** Yes, clear after checkout
**Notes:** User wants standard e-commerce behavior

### What should the empty cart page show?

| Option | Description | Selected |
|--------|-------------|----------|
| Message + browse link (Recommended) | Simple message with link to browse products. | ✓ |
| Recommendations + browse link | Show recommended products or recent views. | |
| Message only | Just the message, no links. | |

**User's choice:** Message + browse link
**Notes:** User wants minimal empty state

---

## Order Status Flow

### What order statuses should the system support?

| Option | Description | Selected |
|--------|-------------|----------|
| 4-status flow (Recommended) | Pending → Processing → Shipped → Delivered. Simple, covers most e-commerce needs. | ✓ |
| 5-status flow | Pending → Confirmed → Processing → Shipped → Delivered. More granular tracking. | |
| 6-status flow (with cancel/refund) | Pending → Processing → Shipped → Delivered + Cancelled + Refunded. Full lifecycle. | |

**User's choice:** 4-status flow (Recommended)
**Notes:** User wants simple, standard status flow

### Who can change order statuses?

| Option | Description | Selected |
|--------|-------------|----------|
| Admin only (Recommended) | Only admins can move orders through the pipeline. Users see status but can't change it. | ✓ |
| Users cancel, admins process | Users can cancel pending orders, admins handle the rest. | |
| Users cancel, admins ship | Users can cancel any non-shipped order, admins handle processing/shipping. | |

**User's choice:** Admin only (Recommended)
**Notes:** User wants admin-controlled order management

### How should order confirmation work after payment?

| Option | Description | Selected |
|--------|-------------|----------|
| Pending after payment (Recommended) | Order immediately enters 'Pending' status after successful payment. Admin then moves to 'Processing'. | ✓ |
| Auto-start Processing | Order enters 'Processing' immediately after payment. Skip the pending step. | |

**User's choice:** Pending after payment (Recommended)
**Notes:** User wants standard order flow

### How should users see their order status?

| Option | Description | Selected |
|--------|-------------|----------|
| Badge + timeline (Recommended) | Status badge on order list + detailed status timeline on order detail page. | ✓ |
| Simple text label | Just a text status label on each order. | |
| Status + estimated delivery | Status with estimated delivery date range. | |

**User's choice:** Badge + timeline (Recommended)
**Notes:** User wants visual status indicators

---

## the agent's Discretion

- Category hierarchy structure (flat vs nested)
- Product search implementation (full-text vs filtering)
- Admin dashboard UI layout for product management
- Shipping cost calculation approach

## Deferred Ideas

None — discussion stayed within phase scope
