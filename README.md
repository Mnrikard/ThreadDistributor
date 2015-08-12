# ThreadDistributor
Multi-threading helper library which will distrubute work among worker threads.

Takes 4 arguments to multithread your application:

1. The method to retrieve more work
2. The method which will handle an individual piece of work
3. The number of worker threads to create
4. The timer interval on which to check for more work (assuming there are available threads)

