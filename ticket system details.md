Sidebar menu
Search
Write
Notifications

Qasas
Home
Library
Profile
Stories
Stats
Following
Arvind Kumar
Arvind Kumar
Gulam Ali H.
Gulam Ali H.
Nick Tune
Nick Tune
Himanshu Singour
Himanshu Singour
Anton Martyniuk
Anton Martyniuk
The Medium Blog
The Medium Blog
Bhargava Koya - Fullstack .NET Developer
Bhargava Koya - Fullstack .NET Developer
Find writers and publications to follow.

See suggestions
You're reading for free via Arvind Kumar's Friend Link. Upgrade to access the best of Medium.

Member-only story

Building a Ticketing System: Concurrency, Locks, and Race Conditions
Arvind Kumar
Arvind Kumar

Following
21 min read
·
Oct 30, 2025
386


5





What happens when 100,000 fans try to book the same concert ticket at exactly 10:00 AM? Let’s design a ticketing system that prevents double-booking, handles flash sales, and maintains data integrity under extreme load.

Full story for non-members | Grab My Microservices E-Book | Youtube | LinkedIn | Book a 1:1 Meeting

Press enter or click to view image in full size

Table of Contents

· A Real-World Problem
· Part 1: The Core Challenge — Race Conditions
· Part 2: Design Discussion
— Step 1: Database Schema
— Step 2: Approach 1 — Pessimistic Locking with Database
— Step 3: Approach 2 — Optimistic Locking
— Step 4: Approach 3 — Distributed Locking with Redis
— Step 5: Advanced Distributed Locking — Redlock Algorithm
— Step 6: The Complete Booking Flow
— Step 7: Handling Edge Cases
· Part 3: System Architecture
· Part 3.1: Search and Discovery (User Experience + APIs)
· Part 3.2: Security Model (AuthN/AuthZ) and Public vs Private APIs
· Part 3.3: Services Decomposition and Scale Justification
· Part 4: Trade-offs and Final Thoughts
· Key Takeaways
· Trade-offs Discussed
· Homework Assignment

A Real-World Problem
Aadvik (Interviewer): “Sara, imagine it’s Taylor Swift’s Eras Tour final show. Tickets go on sale at 10:00 AM sharp. Within the first minute, you have 500,000 users all trying to book the same 50,000 seats. What’s the first problem you think of?”

Sara (Candidate): [Immediately alert] “Race conditions! Multiple users trying to book the same seat simultaneously. Without proper locking, we could sell seat A1 to both User A and User B. That’s a disaster — double booking!”

Aadvik: “Exactly. And this is why specifications like ACID matter. But here’s the twist — we’re building this as a distributed system. Multiple servers, multiple database instances. How do you prevent two different servers from selling the same seat?”

Sara: “That’s the classic distributed systems challenge. We need distributed locks, but also need to handle failures gracefully. And we can’t just lock everything — performance would suffer.”

Aadvik: “Perfect understanding. Let’s design a ticket booking system that handles flash sales, prevents overbooking, and scales horizontally. Ready?”

Sara: “Yes! But let me understand the requirements first. Here are my clarifying questions:

What’s the scale? How many concurrent users during peak?
What types of events? Concerts, movies, sports — do they have different patterns?
What happens if a user abandons booking mid-process?
Do we need to reserve seats temporarily while payment processes?
What about seat preferences? (aisle, front row, etc.)
How long should a ticket reservation hold before expiring?”
Aadvik: “Great questions, Sara. Let’s define the requirements:

Functional Requirements:

Browse available seats for an event
Select and reserve seats temporarily (e.g., 10 minutes for payment)
Confirm booking after payment
Handle seat selection with preferences (aisle, front row, etc.)
Cancel reservations if payment not completed
Support multiple event types (concerts, movies, sports)
Non-Functional Requirements:

Scale: 500,000 concurrent users during flash sales
Zero double-booking — strict consistency required
Latency: <500ms for seat availability check, <2s for reservation
99.9% availability
Handle payment gateway failures gracefully
Support 10,000+ concurrent bookings per second
Scale Estimation:

Traffic Patterns:

Normal: 10,000 seat views/minute, 500 bookings/minute
Flash Sale: 500,000 concurrent users, 50,000 bookings/minute (~833 bookings/sec)
Average booking flow: 2–5 minutes (view → select → payment → confirm)
Data Volume:

Events: 1,000 active events, 10,000 events/year
Average seats per event: 10,000–50,000
Bookings: 1 million bookings/month, 12 million/year
Reservation holds: Peak of 100,000 active reservations”
Part 1: The Core Challenge — Race Conditions
Aadvik: “Let’s start with the fundamental problem. Show me what happens without any locking.”

Sara: “This is a classic race condition scenario. Let me illustrate:

Problem Scenario:

Time  T1: User A checks seat A1 → Available = true
Time  T2: User B checks seat A1 → Available = true (still!)
Time  T3: User A books seat A1 → UPDATE SET status='BOOKED' WHERE seat='A1'
Time  T4: User B books seat A1 → UPDATE SET status='BOOKED' WHERE seat='A1'
Result: Both users think they booked the same seat!

Aadvik: “Perfect. Now, how would you fix this? What are your options?”

Sara: “Several approaches:

Pessimistic Locking — Lock the seat when someone views it
Optimistic Locking — Use version numbers/timestamps
Database Transactions — Atomic operations
Distributed Locks — When multiple servers are involved”
Aadvik: “Let’s explore each one, starting with the simplest.”

Part 2: Design Discussion
Below are the aspects in the design discussion

Database Schema
Different locking mechanisms(Race Condition handling)
System Architecture
Search system
Auth system
Services are split into microservices as per the domain
Trade-off discussion
Step 1: Database Schema
Aadvik: “First, design the database schema. What tables do we need?”

Sara: “We need:

Events table (event details)
Seats table (seat inventory)
Bookings table (confirmed bookings)
Reservations table (temporary holds)”
Database Schema:

-- Events
CREATE TABLE events (
    event_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    event_name VARCHAR(255) NOT NULL,
    event_date DATETIME NOT NULL,
    venue_name VARCHAR(255),
    total_seats INT NOT NULL,
    available_seats INT NOT NULL,
    status ENUM('UPCOMING', 'ON_SALE', 'SOLD_OUT', 'CANCELLED') DEFAULT 'UPCOMING',
    sale_start_time DATETIME,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_sale_start_time (sale_start_time),
    INDEX idx_status (status)
);

-- Seats
CREATE TABLE seats (
    seat_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    event_id BIGINT NOT NULL,
    seat_number VARCHAR(20) NOT NULL,  -- e.g., "A1", "B5"
    section VARCHAR(50),                -- e.g., "VIP", "Balcony"
    row_number VARCHAR(10),
    seat_type ENUM('REGULAR', 'VIP', 'PREMIUM') DEFAULT 'REGULAR',
    price DECIMAL(10, 2) NOT NULL,
    status ENUM('AVAILABLE', 'RESERVED', 'BOOKED', 'BLOCKED') DEFAULT 'AVAILABLE',
    version BIGINT DEFAULT 0,  -- For optimistic locking
    reserved_by VARCHAR(50),   -- User ID or session ID
    reserved_until DATETIME,   -- Expiration time for reservation
    booking_id BIGINT,         -- Foreign key to bookings
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (event_id) REFERENCES events(event_id),
    UNIQUE KEY uk_event_seat (event_id, seat_number),
    INDEX idx_event_status (event_id, status),
    INDEX idx_reserved_until (reserved_until)
);
-- Bookings (Confirmed)
CREATE TABLE bookings (
    booking_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    event_id BIGINT NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    total_amount DECIMAL(10, 2) NOT NULL,
    status ENUM('PENDING', 'CONFIRMED', 'CANCELLED', 'FAILED') DEFAULT 'PENDING',
    payment_id VARCHAR(100),
    payment_status ENUM('PENDING', 'SUCCESS', 'FAILED') DEFAULT 'PENDING',
    booking_reference VARCHAR(50) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    confirmed_at TIMESTAMP NULL,
    FOREIGN KEY (event_id) REFERENCES events(event_id),
    INDEX idx_user_id (user_id),
    INDEX idx_booking_reference (booking_reference),
    INDEX idx_status (status)
);
-- Booking Seats (Many-to-Many)
CREATE TABLE booking_seats (
    booking_seat_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    booking_id BIGINT NOT NULL,
    seat_id BIGINT NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (booking_id) REFERENCES bookings(booking_id),
    FOREIGN KEY (seat_id) REFERENCES seats(seat_id),
    UNIQUE KEY uk_booking_seat (booking_id, seat_id),
    INDEX idx_seat_id (seat_id)
);
-- Reservations (Temporary holds)
CREATE TABLE reservations (
    reservation_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    seat_id BIGINT NOT NULL,
    event_id BIGINT NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    session_id VARCHAR(100),
    expires_at DATETIME NOT NULL,
    status ENUM('ACTIVE', 'CONFIRMED', 'EXPIRED', 'CANCELLED') DEFAULT 'ACTIVE',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (seat_id) REFERENCES seats(seat_id),
    FOREIGN KEY (event_id) REFERENCES events(event_id),
    INDEX idx_seat_id (seat_id),
    INDEX idx_expires_at (expires_at),
    INDEX idx_user_id (user_id)
);
Back-of-the-envelope storage estimation
Assumptions (order-of-magnitude):

Active events on sale: ~1,000; total events/year: ~10,000
Average seats per event: 20,000 (mix of sections/rows)
Bookings/year: ~12M (avg. 1.5 seats/booking → ~18M booking_seats rows)
Peak active reservations: 100k; churned continuously
Approx. per-row sizes (rounded):

events ≈ 1 KB → 10,000 × 1 KB ≈ 10 MB/year
seats ≈ 120 B (ids, status, price, indexes) → 10,000 events × 20k seats ≈ 200M seats/year if fully regenerated; typically reuse layouts, so active footprint ~1,000 × 20k = 20M seats → 20M × 120 B ≈ 2.4 GB active
reservations ≈ 120 B; active window 100k → ~12 MB (ephemeral)
bookings ≈ 200 B → 12M × 200 B ≈ 2.4 GB/year
booking_seats ≈ 120 B → 18M × 120 B ≈ 2.16 GB/year
Envelope (active + annual growth):

Active inventory (seats for on-sale events): ~2.4 GB
Annual bookings data: ~4.5–5 GB/year (bookings + booking_seats)
Events metadata + reservations: negligible in comparison (<100 MB)
Indexes typically add 20–40% overhead → add ~1–2 GB
Rule of thumb: plan for ~7–8 GB/year growth for bookings, keep ~3–4 GB for active seat inventory, and round up to 15 GB/year including indexes and headroom. Multi-year retention or analytics snapshots should move to colder storage/partitions.

Table purposes (why each exists):

events: Canonical record for each show/movie/match. Holds sale windows, total/available seat counts, and lifecycle state (ON_SALE, SOLD_OUT). Used for listings, gating sales start, and analytics at the event level.
seats: The live seat inventory for an event. One row per seat with status (AVAILABLE, RESERVED, BOOKED), pricing, and reservation metadata (reserved_by, reserved_until). This is the single source of truth for availability and where locking/versioning applies.
bookings: The customer order header after successful payment. Tracks user, totals, payment status/IDs, confirmation timestamp, and a unique booking_reference for customer support and reconciliation.
booking_seats: Junction table linking a booking to one or more seats. Stores a snapshot price per seat at purchase time (supports dynamic pricing, discounts, partial refunds) without mutating historical seat data.
reservations: Ephemeral holds while the user pays. Decouples the temporary “hold” from the final “booking”, supports expiry/cleanup, and prevents double‑booking during checkout.
Aadvik: “Why did you include a version field in the seats table?"

Sara: “For optimistic locking! We’ll use it to detect concurrent modifications. When we update a seat, we check if the version matches. If not, someone else modified it.”

Aadvik: “Good. Now let’s solve the race condition. Start with the naive approach.”

Step 2: Approach 1 — Pessimistic Locking with Database
Aadvik: “Show me pessimistic locking using database locks.”

Sara: “Pessimistic locking assumes conflicts will happen, so we lock the resource upfront.”

Press enter or click to view image in full size

@Service
@Transactional
public class SeatBookingService {
    
    @Autowired
    private SeatRepository seatRepository;
    
    @Autowired
    private BookingRepository bookingRepository;
    
    // NAIVE APPROACH - Has race condition!
    public BookingResponse bookSeat(Long eventId, String seatNumber, String userId) {
        // Step 1: Check availability
        Seat seat = seatRepository.findByEventIdAndSeatNumber(eventId, seatNumber);
        
        if (seat == null || !seat.getStatus().equals("AVAILABLE")) {
            throw new SeatNotAvailableException();
        }
        
        // Step 2: Book the seat (RACE CONDITION HERE!)
        seat.setStatus("BOOKED");
        seatRepository.save(seat);
        
        // Step 3: Create booking
        Booking booking = createBooking(eventId, userId, seat);
        
        return new BookingResponse(booking);
    }
}
Aadvik: “What’s wrong with this?”

Sara: “Between checking availability and updating, another request can slip through. We need to lock the row.”

Pessimistic Locking Solution:

@Service
@Transactional
public class SeatBookingService {
    
    @Autowired
    private SeatRepository seatRepository;
    
    // OPTION 1: SELECT FOR UPDATE (Pessimistic Lock)
    public ReservationResponse reserveSeat(Long eventId, String seatNumber, String userId) {
        // This query locks the row until transaction commits
        Seat seat = seatRepository.findByEventIdAndSeatNumberWithLock(eventId, seatNumber);
        
        if (seat == null) {
            throw new SeatNotFoundException();
        }
        
        // Check availability (row is locked, no one else can modify)
        if (!seat.getStatus().equals("AVAILABLE")) {
            throw new SeatNotAvailableException("Seat already booked");
        }
        
        // Reserve for 10 minutes
        seat.setStatus("RESERVED");
        seat.setReservedBy(userId);
        seat.setReservedUntil(LocalDateTime.now().plusMinutes(10));
        seatRepository.save(seat);
        
        // Create reservation record
        Reservation reservation = createReservation(seat, userId);
        
        return new ReservationResponse(reservation);
    }
}

// Repository with Lock
public interface SeatRepository extends JpaRepository<Seat, Long> {
    
    // Standard query (no lock)
    Seat findByEventIdAndSeatNumber(Long eventId, String seatNumber);
    
    // Query with pessimistic lock
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("SELECT s FROM Seat s WHERE s.eventId = :eventId AND s.seatNumber = :seatNumber")
    Seat findByEventIdAndSeatNumberWithLock(@Param("eventId") Long eventId, 
                                             @Param("seatNumber") String seatNumber);
}
Aadvik: “What are the pros and cons of this approach?”

Sara: “Pros:

Simple to implement
Guarantees no double-booking
Works with single database
Cons:

Blocks other requests — If User A is viewing seat A1, User B waits
Deadlock potential — If locking multiple seats
Doesn’t work across multiple database instances
Poor performance at scale — Too many blocked requests”
Aadvik: “Good analysis. What happens in a distributed system with multiple app servers and database replicas?”

Sara: “Ah, that’s the issue. SELECT FOR UPDATE only locks within a single database connection. If Server 1 and Server 2 query different database replicas, both can get the same seat!”

Step 3: Approach 2 — Optimistic Locking
Aadvik: “Let’s try optimistic locking. How does it work?”

Sara: “Optimistic locking assumes conflicts are rare. We check version numbers instead of locking.”

Press enter or click to view image in full size

@Service
@Transactional
public class SeatBookingService {
    
    // OPTIMISTIC LOCKING APPROACH
    public ReservationResponse reserveSeatOptimistic(Long eventId, String seatNumber, String userId) {
        // Step 1: Read seat with current version
        Seat seat = seatRepository.findByEventIdAndSeatNumber(eventId, seatNumber);
        
        if (seat == null || !seat.getStatus().equals("AVAILABLE")) {
            throw new SeatNotAvailableException();
        }
        
        long currentVersion = seat.getVersion(); // e.g., version = 5
        
        // Step 2: Update with version check
        int updated = seatRepository.updateSeatStatusWithVersion(
            seat.getSeatId(),
            "RESERVED",
            currentVersion,  // Only update if version still matches
            userId,
            LocalDateTime.now().plusMinutes(10)
        );
        
        if (updated == 0) {
            // Version mismatch! Someone else modified it
            throw new ConcurrentModificationException("Seat was modified by another user. Please try again.");
        }
        
        return new ReservationResponse(createReservation(seat, userId));
    }
}

// Repository with Optimistic Lock
public interface SeatRepository extends JpaRepository<Seat, Long> {
    
    @Modifying
    @Query("UPDATE Seat s SET s.status = :status, " +
           "s.reservedBy = :userId, " +
           "s.reservedUntil = :reservedUntil, " +
           "s.version = s.version + 1 " +
           "WHERE s.seatId = :seatId " +
           "AND s.version = :expectedVersion " +
           "AND s.status = 'AVAILABLE'")
    int updateSeatStatusWithVersion(
        @Param("seatId") Long seatId,
        @Param("status") String status,
        @Param("expectedVersion") Long expectedVersion,
        @Param("userId") String userId,
        @Param("reservedUntil") LocalDateTime reservedUntil
    );
}
Aadvik: “What happens if the update fails?”

Sara: “The UPDATE returns 0 rows affected, meaning the version changed. We throw an exception and ask the user to retry. This is better than blocking, but…”

Aadvik: “But what?”

Sara: “In a flash sale scenario, retries create more load. 500,000 users all retrying at once? That’s a thundering herd problem. Also, it doesn’t work well for distributed systems across different databases.”

Aadvik: “Exactly. So we need distributed locking. Let’s design it.”

Step 4: Approach 3 — Distributed Locking with Redis
Aadvik: “How would you implement distributed locks using Redis?”

Sara: “Redis can act as a distributed lock coordinator. Multiple servers can check the same Redis instance.”

Basic Redis Lock:

Press enter or click to view image in full size

@Service
public class SeatBookingService {
    
    @Autowired
    private RedisTemplate<String, String> redisTemplate;
    
    @Autowired
    private SeatRepository seatRepository;
    
    private static final String LOCK_PREFIX = "seat:lock:";
    private static final int LOCK_TIMEOUT_SECONDS = 30;
    
    public ReservationResponse reserveSeatWithDistributedLock(
            Long eventId, String seatNumber, String userId) {
        
        String lockKey = LOCK_PREFIX + eventId + ":" + seatNumber;
        String lockValue = UUID.randomUUID().toString();
        
        try {
            // Attempt to acquire lock
            Boolean acquired = redisTemplate.opsForValue().setIfAbsent(
                lockKey, 
                lockValue, 
                Duration.ofSeconds(LOCK_TIMEOUT_SECONDS)
            );
            
            if (!acquired) {
                throw new SeatNotAvailableException("Seat is being processed by another user");
            }
            
            // Lock acquired! Proceed with booking
            try {
                Seat seat = seatRepository.findByEventIdAndSeatNumber(eventId, seatNumber);
                
                if (seat == null || !seat.getStatus().equals("AVAILABLE")) {
                    throw new SeatNotAvailableException();
                }
                
                // Reserve the seat
                seat.setStatus("RESERVED");
                seat.setReservedBy(userId);
                seat.setReservedUntil(LocalDateTime.now().plusMinutes(10));
                seatRepository.save(seat);
                
                return new ReservationResponse(createReservation(seat, userId));
                
            } finally {
                // Release lock using Lua script (atomic)
                releaseLock(lockKey, lockValue);
            }
            
        } catch (Exception e) {
            // Ensure lock is released on error
            releaseLock(lockKey, lockValue);
            throw e;
        }
    }
    
    private void releaseLock(String lockKey, String lockValue) {
        // Lua script ensures we only delete if value matches (prevents deleting other's lock)
        String luaScript = 
            "if redis.call('get', KEYS[1]) == ARGV[1] then " +
            "    return redis.call('del', KEYS[1]) " +
            "else " +
            "    return 0 " +
            "end";
        
        redisTemplate.execute(
            new DefaultRedisScript<>(luaScript, Long.class),
            Collections.singletonList(lockKey),
            lockValue
        );
erse
    }
}
Aadvik: “Why the Lua script for releasing the lock?”

Sara: “Critical! Imagine this scenario:

Server 1 acquires lock, sets value to uuid1
Server 1 takes 35 seconds (lock expires after 30s)
Server 2 acquires lock, sets value to uuid2
Server 1 finishes, tries to delete lock using uuid1
Without Lua script, Server 1 might delete Server 2’s lock!
The Lua script ensures we only delete if the value matches, preventing accidental lock deletion.”

Aadvik: “Excellent. But what if the server crashes while holding the lock?”

Sara: “That’s why we set a TTL (time-to-live). The lock auto-expires. But we need to handle timeout scenarios carefully.”

Step 5: Advanced Distributed Locking — Redlock Algorithm
Aadvik: “For production, Redis recommends Redlock algorithm when using Redis Cluster. Explain it.”

Sara: “Redlock provides stronger guarantees by using multiple Redis instances. Here’s how it works:

Redlock Algorithm:

Client gets current time T1
Try to acquire lock on N Redis instances (N = 5 typical)
Lock is valid if acquired on majority (⌈N/2⌉ + 1)
Calculate lock validity time = TTL — (T2 — T1) — clock drift
Release lock on all instances”
@Component
public class RedlockDistributedLock {
    
    private List<RedisTemplate<String, String>> redisInstances;
    private static final int REDIS_INSTANCE_COUNT = 5;
    private static final int LOCK_TTL_MS = 30000; // 30 seconds
    private static final int CLOCK_DRIFT_MS = 100;
    
    public boolean tryLock(String resource, String value, int ttlMs) {
        long startTime = System.currentTimeMillis();
        int acquiredCount = 0;
        
        // Try to acquire lock on all Redis instances
        for (RedisTemplate<String, String> redis : redisInstances) {
            try {
                Boolean acquired = redis.opsForValue().setIfAbsent(
                    resource, 
                    value, 
                    Duration.ofMillis(ttlMs)
                );
                if (Boolean.TRUE.equals(acquired)) {
                    acquiredCount++;
                }
            } catch (Exception e) {
                // Continue even if one instance fails
                log.warn("Failed to acquire lock on Redis instance", e);
            }
        }
        
        long elapsed = System.currentTimeMillis() - startTime;
        long validityTime = ttlMs - elapsed - CLOCK_DRIFT_MS;
        
        // Lock is valid if acquired on majority and validity time is positive
        boolean isValid = acquiredCount >= (REDIS_INSTANCE_COUNT / 2 + 1) && validityTime > 0;
        
        if (!isValid) {
            // Release any locks we did acquire
            releaseLock(resource, value);
        }
        
        return isValid;
    }
    
    public void releaseLock(String resource, String value) {
        String luaScript = 
            "if redis.call('get', KEYS[1]) == ARGV[1] then " +
            "    return redis.call('del', KEYS[1]) " +
            "else " +
            "    return 0 " +
            "end";
        
        for (RedisTemplate<String, String> redis : redisInstances) {
            try {
                redis.execute(
                    new DefaultRedisScript<>(luaScript, Long.class),
                    Collections.singletonList(resource),
                    value
                );
            } catch (Exception e) {
                log.warn("Failed to release lock on Redis instance", e);
            }
        }
    }
}
Aadvik: “When would you use simple Redis lock vs Redlock?”

Sara: “Simple Redis Lock:

Single Redis instance or Redis Sentinel (high availability)
Lower complexity
Good for most use cases
Tolerates occasional false positives
Redlock:

Redis Cluster environment
Higher consistency requirements
Can tolerate Redis instance failures
More complex, higher latency (need N Redis instances)”
Step 6: The Complete Booking Flow
Aadvik: “Now design the end-to-end booking flow with proper locking.”

Sara: “Let me design a complete flow with reservation → payment → confirmation.”

Press enter or click to view image in full size

Complete Implementation:

@Service
@Slf4j
public class TicketBookingService {
    
    @Autowired
    private SeatRepository seatRepository;
    
    @Autowired
    private BookingRepository bookingRepository;
    
    @Autowired
    private ReservationRepository reservationRepository;
    
    @Autowired
    private RedisTemplate<String, String> redisTemplate;
    
    @Autowired
    private PaymentService paymentService;
    
    private static final String LOCK_PREFIX = "seat:lock:";
    private static final int RESERVATION_TIMEOUT_MINUTES = 10;
    
    /**
     * Step 1: Reserve seats with distributed lock
     */
    @Transactional
    public ReservationResponse reserveSeats(
            Long eventId, List<String> seatNumbers, String userId) {
        
        // Sort seat numbers to prevent deadlock (always lock in same order)
        List<String> sortedSeats = seatNumbers.stream()
            .sorted()
            .collect(Collectors.toList());
        
        List<String> lockKeys = sortedSeats.stream()
            .map(seat -> LOCK_PREFIX + eventId + ":" + seat)
            .collect(Collectors.toList());
        
        String lockValue = UUID.randomUUID().toString();
        List<String> acquiredLocks = new ArrayList<>();
        
        try {
            // Acquire all locks (for multiple seats)
            for (String lockKey : lockKeys) {
                Boolean acquired = redisTemplate.opsForValue().setIfAbsent(
                    lockKey,
                    lockValue,
                    Duration.ofSeconds(30)
                );
                
                if (!acquired) {
                    // Failed to acquire all locks, release what we have
                    releaseLocks(acquiredLocks, lockValue);
                    throw new SeatNotAvailableException(
                        "One or more seats are being processed. Please try again.");
                }
                
                acquiredLocks.add(lockKey);
            }
            
            // All locks acquired! Proceed with reservation
            LocalDateTime reservedUntil = LocalDateTime.now()
                .plusMinutes(RESERVATION_TIMEOUT_MINUTES);
            
            List<Seat> reservedSeats = new ArrayList<>();
            List<Reservation> reservations = new ArrayList<>();
            
            for (String seatNumber : sortedSeats) {
                Seat seat = seatRepository.findByEventIdAndSeatNumber(eventId, seatNumber);
                
                if (seat == null) {
                    releaseLocks(acquiredLocks, lockValue);
                    throw new SeatNotFoundException("Seat not found: " + seatNumber);
                }
                
                if (!seat.getStatus().equals("AVAILABLE")) {
                    releaseLocks(acquiredLocks, lockValue);
                    throw new SeatNotAvailableException(
                        "Seat " + seatNumber + " is no longer available");
                }
                
                // Reserve the seat
                seat.setStatus("RESERVED");
                seat.setReservedBy(userId);
                seat.setReservedUntil(reservedUntil);
                seat = seatRepository.save(seat);
                reservedSeats.add(seat);
                
                // Create reservation record
                Reservation reservation = new Reservation();
                reservation.setSeatId(seat.getSeatId());
                reservation.setEventId(eventId);
                reservation.setUserId(userId);
                reservation.setExpiresAt(reservedUntil);
                reservation.setStatus("ACTIVE");
                reservation = reservationRepository.save(reservation);
                reservations.add(reservation);
            }
            
            // Schedule cleanup job for expired reservations
            scheduleReservationCleanup(reservedUntil);
            
            return new ReservationResponse(reservations, reservedUntil);
            
        } finally {
            // Always release locks
            releaseLocks(acquiredLocks, lockValue);
        }
    }
    
    /**
     * Step 2: Confirm booking after payment
     */
    @Transactional
    public BookingResponse confirmBooking(
            String reservationId, PaymentRequest paymentRequest) {
        
        Reservation reservation = reservationRepository.findById(reservationId)
            .orElseThrow(() -> new ReservationNotFoundException());
        
        // Check if reservation expired
        if (LocalDateTime.now().isAfter(reservation.getExpiresAt())) {
            throw new ReservationExpiredException("Reservation expired. Please select seats again.");
        }
        
        if (!reservation.getStatus().equals("ACTIVE")) {
            throw new InvalidReservationException("Reservation is not active");
        }
        
        String lockKey = LOCK_PREFIX + reservation.getEventId() + ":" + 
                         getSeatNumber(reservation.getSeatId());
        String lockValue = UUID.randomUUID().toString();
        
        try {
            // Re-acquire lock for confirmation
            Boolean acquired = redisTemplate.opsForValue().setIfAbsent(
                lockKey,
                lockValue,
                Duration.ofSeconds(30)
            );
            
            if (!acquired) {
                throw new SeatNotAvailableException("Seat is being processed");
            }
            
            // Process payment
            PaymentResponse payment = paymentService.processPayment(paymentRequest);
            
            if (!payment.isSuccess()) {
                throw new PaymentFailedException("Payment failed: " + payment.getErrorMessage());
            }
            
            // Confirm booking
            Seat seat = seatRepository.findById(reservation.getSeatId())
                .orElseThrow(() -> new SeatNotFoundException());
            
            if (!seat.getStatus().equals("RESERVED") || 
                !seat.getReservedBy().equals(reservation.getUserId())) {
                throw new InvalidSeatStateException("Seat state changed");
            }
            
            // Create booking
            Booking booking = new Booking();
            booking.setEventId(reservation.getEventId());
            booking.setUserId(reservation.getUserId());
            booking.setTotalAmount(seat.getPrice());
            booking.setStatus("CONFIRMED");
            booking.setPaymentId(payment.getPaymentId());
            booking.setPaymentStatus("SUCCESS");
            booking.setBookingReference(generateBookingReference());
            booking.setConfirmedAt(LocalDateTime.now());
            booking = bookingRepository.save(booking);
            
            // Link seat to booking
            BookingSeat bookingSeat = new BookingSeat();
            bookingSeat.setBookingId(booking.getBookingId());
            bookingSeat.setSeatId(seat.getSeatId());
            bookingSeat.setPrice(seat.getPrice());
            bookingSeatRepository.save(bookingSeat);
            
            // Update seat status
            seat.setStatus("BOOKED");
            seat.setBookingId(booking.getBookingId());
            seat.setReservedBy(null);
            seat.setReservedUntil(null);
            seatRepository.save(seat);
            
            // Update reservation
            reservation.setStatus("CONFIRMED");
            reservationRepository.save(reservation);
            
            // Update event available seats count
            updateEventSeatCount(reservation.getEventId(), -1);
            
            return new BookingResponse(booking);
            
        } finally {
            releaseLock(lockKey, lockValue);
        }
    }
    
    /**
     * Step 3: Cleanup expired reservations (Scheduled job)
     */
    @Scheduled(fixedRate = 60000) // Run every minute
    public void cleanupExpiredReservations() {
        List<Reservation> expiredReservations = reservationRepository
            .findExpiredReservations(LocalDateTime.now());
        
        for (Reservation reservation : expiredReservations) {
            try {
                releaseExpiredReservation(reservation);
            } catch (Exception e) {
                log.error("Failed to release expired reservation: " + reservation.getReservationId(), e);
            }
        }
    }
    
    private void releaseExpiredReservation(Reservation reservation) {
        String lockKey = LOCK_PREFIX + reservation.getEventId() + ":" + 
                         getSeatNumber(reservation.getSeatId());
        
        // Try to acquire lock (may fail if someone is booking it concurrently)
        String lockValue = UUID.randomUUID().toString();
        Boolean acquired = redisTemplate.opsForValue().setIfAbsent(
            lockKey,
            lockValue,
            Duration.ofSeconds(5)
        );
        
        if (acquired) {
            try {
                Seat seat = seatRepository.findById(reservation.getSeatId()).orElse(null);
                if (seat != null && seat.getStatus().equals("RESERVED") &&
                    LocalDateTime.now().isAfter(seat.getReservedUntil())) {
                    
                    seat.setStatus("AVAILABLE");
                    seat.setReservedBy(null);
                    seat.setReservedUntil(null);
                    seatRepository.save(seat);
                    
                    reservation.setStatus("EXPIRED");
                    reservationRepository.save(reservation);
                    
                    updateEventSeatCount(reservation.getEventId(), 1);
                }
            } finally {
                releaseLock(lockKey, lockValue);
            }
        }
    }
    
    private void releaseLocks(List<String> lockKeys, String lockValue) {
        for (String lockKey : lockKeys) {
            releaseLock(lockKey, lockValue);
        }
    }
    
    private void releaseLock(String lockKey, String lockValue) {
        String luaScript = 
            "if redis.call('get', KEYS[1]) == ARGV[1] then " +
            "    return redis.call('del', KEYS[1]) " +
            "else " +
            "    return 0 " +
            "end";
        
        redisTemplate.execute(
            new DefaultRedisScript<>(luaScript, Long.class),
            Collections.singletonList(lockKey),
            lockValue
        );
    }
    
    private void updateEventSeatCount(Long eventId, int delta) {
        // Optimistic update with retry
        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++) {
            Event event = eventRepository.findById(eventId).orElse(null);
            if (event != null) {
                int newCount = event.getAvailableSeats() + delta;
                int updated = eventRepository.updateAvailableSeats(
                    eventId, 
                    event.getVersion(), 
                    newCount
                );
                if (updated > 0) break;
            }
        }
    }
}
Step 7: Handling Edge Cases
Aadvik: “What happens if payment succeeds but database update fails?”

Sara: “That’s critical! We need idempotency and compensating transactions. Let me design a solution.”

Saga Pattern for Payment:

@Service
public class BookingSagaService {
    
    // Use event sourcing or state machine
    public void handlePaymentSuccess(String reservationId, PaymentResponse payment) {
        try {
            // Attempt to confirm booking
            confirmBooking(reservationId, payment);
            
        } catch (Exception e) {
            // Compensating action: Refund payment
            log.error("Booking confirmation failed, initiating refund", e);
            paymentService.refundPayment(payment.getPaymentId());
            
            // Release reservation
            releaseReservation(reservationId);
            
            throw new BookingFailureException("Booking failed, payment refunded", e);
        }
    }
}
Idempotency Key:

@Service
public class IdempotentBookingService {
    
    @Autowired
    private RedisTemplate<String, String> redisTemplate;
    
    public BookingResponse confirmBookingWithIdempotency(
            String idempotencyKey, 
            String reservationId, 
            PaymentRequest paymentRequest) {
        
        // Check if this request was already processed
        String existingBooking = redisTemplate.opsForValue().get("idempotency:" + idempotencyKey);
        if (existingBooking != null) {
            // Return existing booking
            return bookingRepository.findById(existingBooking)
                .map(this::toBookingResponse)
                .orElseThrow();
        }
        
        // Process booking
        BookingResponse response = confirmBooking(reservationId, paymentRequest);
        
        // Store idempotency key
        redisTemplate.opsForValue().set(
            "idempotency:" + idempotencyKey,
            response.getBookingId(),
            Duration.ofHours(24)
        );
        
        return response;
    }
}
Part 3: System Architecture
Press enter or click to view image in full size

Part 3.1: Search and Discovery (User Experience + APIs)
Aadvik: “Seat booking is great, but users first discover events. Let’s add Search.”

Sara: “We’ll keep it simple and fast: full-text search on events with faceted filtering.”

User flows
Browse trending/featured events (unauthenticated allowed)
Search by keywords, city, venue, date range, category (concerts, sports, movies)
View event details, see seat map and availability snapshot
Search API (read-only)
GET /api/search?q=taylor+swift&city=seattle&dateFrom=2025-11-01&dateTo=2025-12-31&category=CONCERT&page=0&size=20
→ 200 OK
{
  "results": [
    { "eventId": 987, "name": "Taylor Swift – Eras Tour", "city": "Seattle", "date": "2025-12-05T02:00:00Z", "venue": "Lumen Field", "category": "CONCERT", "minPrice": 79.0, "availability": "ON_SALE" }
  ],
  "page": { "number": 0, "size": 20, "totalElements": 124, "totalPages": 7 }
}
Index design (Elasticsearch/OpenSearch) — minimal
{
  "index": "events",
  "mappings": {
    "properties": {
      "eventId": { "type": "long" },
      "name": { "type": "text", "analyzer": "standard", "fields": { "keyword": { "type": "keyword" } } },
      "city": { "type": "keyword" },
      "venue": { "type": "keyword" },
      "category": { "type": "keyword" },
      "date": { "type": "date" },
      "minPrice": { "type": "double" },
      "availability": { "type": "keyword" }
    }
  }
}
Minimal Java stubs (indicators only)
@RestController
@RequestMapping("/api/search")
public class SearchController {
    @GetMapping
    public Page<EventSummary> search(@RequestParam String q,
                                     @RequestParam(required=false) String city,
                                     @RequestParam(required=false) String category,
                                     @RequestParam(required=false) @DateTimeFormat(iso=ISO.DATE) LocalDate dateFrom,
                                     @RequestParam(required=false) @DateTimeFormat(iso=ISO.DATE) LocalDate dateTo,
                                     Pageable pageable) {
        return Page.empty(); // placeholder – to be implemented
    }
}

public record EventSummary(Long eventId, String name, String city, String venue,
                           String category, Instant date, double minPrice, String availability) {}
Search Request Flow (Sequence)
Press enter or click to view image in full size

Indexing Pipeline (Events → Search)

Part 3.2: Security Model (AuthN/AuthZ) and Public vs Private APIs
Aadvik: “Search is public, but booking isn’t. Define security clearly.”

Sara: “We’ll use JWT-based auth. Anonymous users can browse/search; authenticated users can reserve and book. Admins manage events and pricing.”

Access policy
Public (no login required):

GET /api/search (search events)
GET /api/events (list) and /api/events/{id} (details, seat map snapshot)
Authenticated user required:

POST /api/reservations (reserve seats)
POST /api/bookings/confirm (confirm after payment)
GET /api/bookings/me (my bookings)
Admin only:

POST /api/events (create/update event)
POST /api/events/{id}/pricing (dynamic pricing rules)
Login & token issuance (JWT)
POST /api/auth/login
{ "email": "sara@example.com", "password": "••••••" }
→ 200 OK
{ "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", "expiresIn": 3600 }
Minimal Java stubs
@RestController
@RequestMapping("/api/auth")
public class AuthController {
    @PostMapping("/login")
    public TokenResponse login(@RequestBody LoginRequest req) {
        return new TokenResponse("<jwt>", 3600); // placeholder
    }
}

public record LoginRequest(String email, String password) {}
public record TokenResponse(String accessToken, long expiresIn) {}
@RestController
@RequestMapping("/api/bookings")
public class BookingController {
    @GetMapping("/me")
    public List<BookingSummary> myBookings(@RequestHeader("Authorization") String bearer) {
        // verify JWT later; placeholder
        return List.of();
    }
}
public record BookingSummary(String bookingReference, Instant date, double total) {}
Authentication sequence (login)
Press enter or click to view image in full size

Authorization sequence (protected request)
Press enter or click to view image in full size

Part 3.3: Services Decomposition and Scale Justification
Aadvik: “At peak scale, which services do we need and why?”

Sara: “We’ll separate hot paths and ownership boundaries to scale independently.”

Proposed services
API Gateway: Single entry; rate limiting, auth, routing
Auth Service: Login, JWT issuance, user management
Search Service: OpenSearch queries, result caching
Events Service: Event lifecycle, metadata, pricing rules
Inventory Service: Seats state machine (AVAILABLE/RESERVED/BOOKED), reservation holds, cleanup
Booking Service: Orchestrates reservation → payment → confirmation; idempotency; writes bookings/booking_seats
Payment Adapter: Isolates payment gateway specifics; retries, webhook handling
Indexer: CDC/outbox → OpenSearch indexing
Notification Service: Emails/SMS for confirmations
Why this split?
Throughput isolation: Search QPS >> Booking QPS; scale independently
Consistency boundaries: Inventory and Booking enforce strong consistency; Search is eventual
Operational ownership: Teams own clear domains; safer deployments
Service topology
Press enter or click to view image in full size

Minimal controller stubs (indicators only)
@RestController
@RequestMapping("/api/events")
public class EventsController {
    @GetMapping
    public Page<EventSummary> list(Pageable pageable) { return Page.empty(); }
    @GetMapping("/{id}")
    public EventSummary get(@PathVariable Long id) { return null; }
}

@RestController
@RequestMapping("/api/inventory")
public class InventoryController {
    @PostMapping("/reservations")
    public ReservationResponse reserve(@RequestBody ReservationRequest req,
                                       @RequestHeader("Authorization") String bearer) {
        return null; // placeholder
    }
}
public record ReservationRequest(Long eventId, List<String> seats) {}
Database alignment after service split (who owns what)
Aadvik: “With services separated, how do our original tables map to service databases?”

Sara: “We keep strong consistency boundaries and avoid cross-DB foreign keys. Services communicate by IDs and events (outbox/CDC).”

Ownership map
Events DB (owned by Events Service):

events (canonical event metadata, sale window, status)
event_pricing_rules (optional; dynamic pricing config)
outbox_events (for Search indexing/Kafka)
Inventory DB (owned by Inventory Service):

seats (authoritative seat state: AVAILABLE/RESERVED/BOOKED, price at time-of-offer)
reservations (temporary holds with TTL)
seat_audit (optional; history of state transitions)
Bookings DB (owned by Booking Service):

bookings (order header: user, totals, payment status)
booking_seats (order lines with per-seat price snapshot)
idempotency_keys (optional; confirm idempotency)
payment_transactions (optional; gateway refs, webhook state)
Auth DB (owned by Auth Service):

users, roles, user_roles, sessions (optional)
Search Index (owned by Search Service):

OpenSearch index events (denormalized view of events for discovery)
Why this alignment?
Seats/reservations require strict locking → isolated in Inventory DB.
Orders, refunds, and idempotency are booking concerns → Bookings DB.
Event metadata changes shouldn’t block seat sales → Events DB decoupled, async to Search.
Search is read-optimized and eventually consistent → OpenSearch.
Schemas regrouped by service (same tables, new home)
-- Events DB (events)
CREATE TABLE events (
  event_id BIGINT PRIMARY KEY,
  event_name VARCHAR(255) NOT NULL,
  event_date DATETIME NOT NULL,
  venue_name VARCHAR(255),
  total_seats INT NOT NULL,
  status ENUM('UPCOMING','ON_SALE','SOLD_OUT','CANCELLED') NOT NULL,
  sale_start_time DATETIME,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_sale_start_time (sale_start_time),
  INDEX idx_status (status)
);

-- Inventory DB (inventory)
CREATE TABLE seats (
  seat_id BIGINT PRIMARY KEY,
  event_id BIGINT NOT NULL,           -- reference by ID (no cross-DB FK)
  seat_number VARCHAR(20) NOT NULL,
  section VARCHAR(50), row_number VARCHAR(10),
  seat_type ENUM('REGULAR','VIP','PREMIUM') DEFAULT 'REGULAR',
  price DECIMAL(10,2) NOT NULL,
  status ENUM('AVAILABLE','RESERVED','BOOKED','BLOCKED') DEFAULT 'AVAILABLE',
  version BIGINT DEFAULT 0,
  reserved_by VARCHAR(50),
  reserved_until DATETIME,
  booking_id BIGINT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uk_event_seat (event_id, seat_number),
  INDEX idx_event_status (event_id, status),
  INDEX idx_reserved_until (reserved_until)
);
CREATE TABLE reservations (
  reservation_id BIGINT PRIMARY KEY,
  seat_id BIGINT NOT NULL,
  event_id BIGINT NOT NULL,
  user_id VARCHAR(50) NOT NULL,
  expires_at DATETIME NOT NULL,
  status ENUM('ACTIVE','CONFIRMED','EXPIRED','CANCELLED') DEFAULT 'ACTIVE',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_seat_id (seat_id),
  INDEX idx_expires_at (expires_at),
  INDEX idx_user_id (user_id)
);
-- Bookings DB (booking)
CREATE TABLE bookings (
  booking_id BIGINT PRIMARY KEY,
  event_id BIGINT NOT NULL,           -- reference by ID (no cross-DB FK)
  user_id VARCHAR(50) NOT NULL,
  total_amount DECIMAL(10,2) NOT NULL,
  status ENUM('PENDING','CONFIRMED','CANCELLED','FAILED') DEFAULT 'PENDING',
  payment_id VARCHAR(100),
  payment_status ENUM('PENDING','SUCCESS','FAILED') DEFAULT 'PENDING',
  booking_reference VARCHAR(50) UNIQUE NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  confirmed_at TIMESTAMP NULL,
  INDEX idx_user_id (user_id),
  INDEX idx_booking_reference (booking_reference),
  INDEX idx_status (status)
);
CREATE TABLE booking_seats (
  booking_seat_id BIGINT PRIMARY KEY,
  booking_id BIGINT NOT NULL,
  seat_id BIGINT NOT NULL,
  price DECIMAL(10,2) NOT NULL,
  UNIQUE KEY uk_booking_seat (booking_id, seat_id),
  INDEX idx_seat_id (seat_id)
);
Cross-service data flow (ER/topology)
Press enter or click to view image in full size

Notes
No cross-DB foreign keys; referential integrity enforced at application layer.
Outbox/CDC from Events → Search; optional CDC from Inventory → analytics.
Booking references seats by IDs; validations happen via Inventory service APIs (not DB joins).
Part 4: Trade-offs and Final Thoughts
Key Takeaways
What we learned:

Concurrency control is critical — Race conditions cause double-booking
Distributed locking — Redis for coordinating across multiple servers
Two-phase booking — Reservation → Payment → Confirmation
Idempotency — Prevent duplicate bookings from retries
Compensating transactions — Handle partial failures gracefully
Trade-offs Discussed
1. Pessimistic vs Optimistic Locking:

Pessimistic: Guarantees consistency, but blocks requests
Optimistic: Better performance, but requires retries
2. Simple Redis Lock vs Redlock:

Simple: Faster, simpler, good for most cases
Redlock: Stronger guarantees, handles Redis failures
3. Lock Duration:

Short (5–10s): Less blocking, but risk of premature release
Long (30s+): Safer, but more blocking
4. Reservation Timeout:

Short (5 min): More inventory turnover, better for users
Long (15 min): Less pressure, worse user experience during flash sales
Homework Assignment
Build a working implementation:

Implement distributed locking with Redis
Create reservation system with TTL
Add idempotency for booking confirmation
Test with concurrent requests (use JMeter or Gatling)
Measure lock contention and optimize
Next topic : Messaging App — introducing real-time communication, WebSockets, and message delivery guarantees.

=======

Check the list below for more system design deep dives like above

Arvind Kumar
Arvind Kumar

10-Days System Design Crash Course
View list
3 stories



Don’t forget to clap the story if you found this useful and follow me for more such stories

System Design Interview
Distributed Systems
System Design Concepts
Software Engineering
Concurrency
386


5




Arvind Kumar
Written by Arvind Kumar
2K followers
·
92 following
Staff Engineer | System Design, Microservices, Java, SpringBoot, Kafka, DBs, AWS, GenAI | Teaching concepts via stories & characters | linkedin.com/in/codefarm0


Following
Responses (5)
Qasas
Qasas
﻿

Cancel
Respond
mohamad shahkhajeh
mohamad shahkhajeh

6 days ago


Have you considered how network partitioning might affect distributed locks and risk temporary double-booking?
14


1 reply

Reply

Bhvsaraf
Bhvsaraf

5 hours ago


Great article!.
confirmBooking(String reservationId, PaymentRequest request)
felt a bit out of place since the reservation object in your case refers to 1 seat for a given event. So practically, if i am booking 3 tickets at an event, your logic…more
1


1 reply

Reply

Mansar Aicha
Mansar Aicha

1 day ago


Great work, thank you for sharing. I'm thinking how we can deploy this system based on azure infrastructure ?
2


1 reply

Reply

See all responses
More from Arvind Kumar
300+ Spring Boot interview questions
Arvind Kumar
Arvind Kumar

300+ Spring Boot interview questions
Comprehensive list of 300+ Spring Boot interview questions tailored for a Microservices Backend Engineer role and , covering all important…

Apr 16
371
9


🧵 Thread-Safe by Design: Concurrency-Aware Patterns Every Java Engineer Should Know (2025 Edition)
Arvind Kumar
Arvind Kumar

🧵 Thread-Safe by Design: Concurrency-Aware Patterns Every Java Engineer Should Know (2025 Edition)
In the world of modern Java, writing multithreaded code isn’t just about using synchronized and hoping for the best. With CPUs scaling…
Jul 7
27


Kafka Bootcamp (Part 1) — From Zero to Production
Arvind Kumar
Arvind Kumar

Kafka Bootcamp (Part 1) — From Zero to Production
Duration: 15 Days (1 hour/day)

Oct 29
38
2


How to Design an Elevator System — Interview Deep Dive
Arvind Kumar
Arvind Kumar

How to Design an Elevator System — Interview Deep Dive
Complete Guide: Elevator State Management, Multi-Elevator Coordination, Request Handling, and Concurrency

Oct 15
57


See all from Arvind Kumar
Recommended from Medium
JUnit 5 is dead, long live JUnit 6!
Javarevisited
In

Javarevisited

by

Erwan LE TUTOUR

JUnit 5 is dead, long live JUnit 6!
Where the tradition of testing meets the spirit of modern development
Oct 30
43
3


99% of Senior Java Developers Can’t Answer These Multithreading Questions.
Stackademic
In

Stackademic

by

Gaddam.Naveen

99% of Senior Java Developers Can’t Answer These Multithreading Questions.
if you are not a medium member then Click here to read free

Oct 31
300
1


I Can’t Tell the Difference Between Java Generic Symbols: T, E, K, V, ?
JavaScript in Plain English
In

JavaScript in Plain English

by

Umesh Kumar Yadav

I Can’t Tell the Difference Between Java Generic Symbols: T, E, K, V, ?
Today I want to talk to you about those dazzling symbols in Java generics — T, E, K, V, and ?.

Oct 26
222
9


Spring Data JPA vs JDBC: The Performance Showdown Every Spring Boot Developer Needs to See 🚀
Raju Methuku
Raju Methuku

Spring Data JPA vs JDBC: The Performance Showdown Every Spring Boot Developer Needs to See 🚀
I ran 6 real performance tests with 40,000+ records. The results changed everything I thought I knew about databases.
Oct 26
101
5


12 Essential Java Best Practices Every Developer Should Follow
Code With Sunil | Code Smarter, not harder
Code With Sunil | Code Smarter, not harder

12 Essential Java Best Practices Every Developer Should Follow
If you are not a Member — Read for free here

6d ago
19
2


Dynamic Request Bodies in Spring Boot (With One Clean Endpoint)
Noyan Germiyanoğlu
Noyan Germiyanoğlu

Dynamic Request Bodies in Spring Boot (With One Clean Endpoint)
In real products, APIs often accept multiple shapes of JSON for the same action (e.g., sending Email/SMS/Push notifications). You want one…
Oct 21
66
2


See more recommendations
Help

Status

About

Careers

Press

Blog

Privacy

Rules

Terms

Text to speech