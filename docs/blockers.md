# MyDesk Blockers

**Last Updated:** July 2026
**Owner:** Autonomous agentic run (Phase 7)
**Rule:** Codeable tasks proceed without pause. Only hard environmental gaps block.

---

## B1 — No mobile build/run environment (CRITICAL, non-codeable)

**What's needed to unblock:**
- macOS + Xcode for iOS builds/simulator
- Android SDK + emulator (or physical device) for Android
- Expo EAS account (`eas build` / `eas submit`) with build credentials
- Apple Developer Program membership (paid) — for TestFlight + App Store
- Google Play Console account + signing key — for Play Store beta/launch
- Firebase project (FCM + Crashlytics) — for push notifications & crash reporting
- Sentry project (or `sentry-expo` DSN) — for crash reporting

**Impact:** Tasks requiring device compilation, emulator execution, store upload, or physical-device QA cannot be *run/verified* on this Windows machine. All such code is authored and committed but not executed.

**Affected tasks:** 113–140 (E2E/device testing, TestFlight, Play beta, store submissions, launch). Authored, not executed.

**Mitigation in place:** Code written against Expo + React Native Paper + SQLite + FCM types; `npm install` + `jest` unit tests run on Windows to validate logic without a device.

---

## B2 — Undecided external dependency (per DEPENDENCIES-AND-BLOCKERS.md)

- Framework decision (React Native vs Flutter) — plan already chose **React Native**, proceeding with that default.
- PDF library (QuestPDF/iText) — web-side concern, not blocking mobile.
- AI model selection (Phase 8) — out of Phase 7 scope.

No action required; proceeding with planned React Native default.

---

## Status

| Blocker | Severity | Codeable work blocked? | Status |
|---------|----------|------------------------|--------|
| B1 Mobile build env | Critical | No (only run/verify) | Logged, proceeding |
| B2 External decisions | Low | No | Resolved with plan defaults |
