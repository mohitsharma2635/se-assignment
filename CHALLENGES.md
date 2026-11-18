# Challenges Faced During Assessment

## 1. CORS error when clicking "Start"

**Problem**

After starting the backend locally with `dotnet run` and the frontend with `npm start` on port 3001, clicking **Start** on the home page did nothing. The browser console showed a CORS error, and the `POST /Plan` request in the Network tab failed with no response data.

**Root cause analysis**

- The CORS policy in `Program.cs` was set up correctly and already allowed `http://localhost:3001`, so the policy itself was not the issue.
- The `RL.Backend` profile in `launchSettings.json` starts the API on two URLs: `http://localhost:10010` and `https://localhost:10011`.
- Because an HTTPS port was available, `app.UseHttpsRedirection()` redirected every HTTP request to HTTPS. It is registered **before** `app.UseCors(...)` in the pipeline.
- The browser sends a preflight `OPTIONS` request before the cross-origin `POST`. The backend answered that preflight with `307 Temporary Redirect` to `https://localhost:10011/Plan` instead of returning the CORS headers.
- Browsers do not follow redirects on a preflight request, so the request was blocked and reported as a CORS error.
- This does not happen in the Docker setup because the container only listens on HTTP, so there is nothing to redirect to.

I confirmed this by sending the preflight request manually with `curl`:

```bash
curl -i -X OPTIONS http://localhost:10010/Plan \
  -H "Origin: http://localhost:3001" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type"
# HTTP/1.1 307 Temporary Redirect
# Location: https://localhost:10011/Plan
```

**Fix**

I ran the backend on HTTP only, matching how it runs in Docker:

```bash
dotnet run --urls http://localhost:10010
```

With no HTTPS port bound, the HTTPS redirection has nothing to redirect to, so the preflight now returns `204 No Content` with the correct `Access-Control-Allow-Origin: http://localhost:3001` header. Clicking **Start** now creates a plan as expected.

**Why this approach**

I chose not to change any code for this, because it is a local environment issue and not part of the assignment. Changing the middleware order or disabling HTTPS redirection in `Program.cs` would also work, but it would add an unrelated change to the submission.

## 2. Backend killed on startup (exit code 137) after rebuilding

**Problem**

After adding the migration, `dotnet run` for the backend stopped working. The process exited straight away with code `137` (killed) and printed nothing, not even a build error.

**Root cause analysis**

- Running the compiled DLL directly with `dotnet bin/Debug/net6.0/RL.Backend.dll` worked fine, so the code and the build were not the problem.
- `dotnet run` starts the app through a small native launcher binary (`bin/Debug/net6.0/RL.Backend`).
- I had rebuilt the project (through `dotnet ef migrations add`) while an older instance of the backend was still running from that same launcher. The build overwrote the launcher file in place.
- macOS caches the code signature of a binary that has already been executed. When that file changes in place, macOS treats the signature as invalid and kills any new process started from it with `SIGKILL`, which shows up as exit code `137`.

**Fix**

I replaced the launcher with a fresh copy so macOS sees it as a new file:

```bash
cd Interview/RL.Backend/bin/Debug/net6.0
cp RL.Backend RL.Backend.tmp && mv RL.Backend.tmp RL.Backend
```

After that, `dotnet run` started normally. Running `dotnet clean` followed by `dotnet build` should also fix it, since that recreates the file from scratch. To avoid this, I now stop the running backend before running EF Core commands or rebuilding.
