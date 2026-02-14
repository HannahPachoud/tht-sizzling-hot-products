## How to Run

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

```bash
# Run the API
cd src
dotnet run
```

The API starts at `http://localhost:5205`.

```bash
# Run the tests
cd tests
dotnet test
```

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/sizzling-hot/daily` | Top sizzling hot product for each day in the dataset |
| `GET` | `/sizzling-hot/last-3-days` | Single top sizzling hot product over the rolling 3-day window |

How to test/call these endpoints when running locally:
```bash
curl -s http://localhost:5205/sizzling-hot/daily
curl -s http://localhost:5205/sizzling-hot/last-3-days
```

## Architecture

The solution uses a layered architecture, with clear separation of concerns:

- **Controller** (`SizzlingHotController`) Accepts HTTP requests, delegates to the service, returns responses, doesn't handle any business logic
- **Service** (`SalesService`) Handles all business logic 
- **Repository** (`OrderRepository`) Loads and deserializes JSON files from the `inputs/` folder and returns the domain objects - in a production system this might look a bit different, for example calling a real database, but what it returns would stay the same



## Assumptions

* No pagination on the daily endpoint. The dataset is small enough to return in a single response. For a larger dataset or unbounded history, I would add `page`/`pageSize` query parameters, or a date range filter (`from`/`to`).

* The UI might ocassionally want to get GetDailyHotProducts without GetHotProductLastThreeDays, and vice versa, which is why I have split it up into two endpoints. Depending on the use cases, it might make more sense to have them both returned from one endpoint, or to keep them as is. 

* "Past 3 days" includes today, the window is `today - 2` to `today` (i.e. 19/07, 20/07, 21/07).

* Cancellations apply to the entire order, the input data format does not support partial cancellations (cancelling individual products within an order), so cancelled order removes all product sales from that order

* Dates are returned in ISO 8601 format (`yyyy-MM-dd`) in API responses, regardless of the `dd/MM/yyyy` format used in the input files

* If today has no sales yet, the endpoint still returns results for days that do have data. A front-end could choose to show the previous day's winner as a fallback.

## Further Considerations

Caching: For a production system, the calculated results could be cached since the underlying data doesn't change frequently, but cancellations can change a previous day's winner, so the cache would need to be invalidated when new cancellations come in, and the current days cache would need to be invalidated when new orders came in, so 
would need to think about if this was worth it

Data re-fetching for the rolling window: The 3-day window is recalculated from scratch on each request. This is the right approach at this data scale because cancellations can change historical results, but at a significantly larger scale, an event-driven approach (recalculating only when orders change) would be worth considering

Single endpoint alternative: Both endpoints could potentially be served by a single endpoint that returns the last N days of winners, giving the front-end everything it needs in one call, both the daily history and enough data to derive the 3-day top product client side.

Repository separation: Products and orders are served from the same `IOrderRepository`. If the data for these were fetched from different locations/DBs in production, splitting into `IProductRepository` and `IOrderRepository` would make sense

Newtonsoft.Json could have been used instead of the custom date converter, but it doesn't support date only, which I feel is better for this use case
