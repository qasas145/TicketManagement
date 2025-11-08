import io.gatling.core.Predef._
import io.gatling.http.Predef._
import scala.concurrent.duration._

class TicketBookingSimulation extends Simulation {

  val httpProtocol = http
    .baseUrl("http://localhost:5000")
    .acceptHeader("application/json")
    .contentTypeHeader("application/json")

  val login = exec(
    http("Login")
      .post("/api/auth/login")
      .body(StringBody("""{"email":"test@example.com","password":"password123"}"""))
      .check(status.is(200))
      .check(jsonPath("$.accessToken").saveAs("accessToken"))
  )

  val reserveSeat = exec(
    http("Reserve Seat")
      .post("/api/inventory/reservations")
      .header("Authorization", "Bearer ${accessToken}")
      .body(StringBody("""{"eventId":1,"seatNumbers":["A${threadId}"]}"""))
      .check(status.in(200, 409))
  )

  val scn = scenario("Concurrent Ticket Booking")
    .exec(login)
    .pause(1)
    .exec(reserveSeat)

  setUp(
    scn.inject(
      rampUsers(1000) during (60 seconds),
      constantUsersPerSec(100) during (5 minutes)
    )
  ).protocols(httpProtocol)
    .assertions(
      global.responseTime.max.lt(2000),
      global.successfulRequests.percent.gt(95)
    )
}

