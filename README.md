# PC_User_Detection

A Windows desktop app that checks whether the person sitting at the PC is a
registered user. It captures a frame from the webcam, generates a face
embedding for it, and compares that against the embeddings of every
registered user image. If any pair is similar enough, the user is verified.

It can also email you the photo when the person is not recognised. That is off
until you configure it; see [Email alerts](#email-alerts).

## Requirements

- Windows (the app uses WinForms and DirectShow, so it is Windows-only)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer to build
- A webcam

The project targets `net8.0-windows` with `RollForward=LatestMajor`, so it also
runs on a machine that only has a newer major runtime installed.

## Getting started

```bash
git clone https://github.com/Jccqt/PC_User_Detection.git
cd PC_User_Detection
dotnet build PCUserDetection.sln
```

Then run the app:

```bash
dotnet run --project PCUserDetection/PCUserDetection.csproj
```

Running it from Visual Studio with F5, or by launching the built executable
directly, works the same way. The image folders are located relative to the
executable rather than the working directory, so it does not matter where the
app is started from.

The face recognition models are pulled in through the `FaceAiSharp.Bundle`
NuGet package and copied into the build output automatically. There is nothing
to download by hand.

## Using the app

Everything lives in one window. The rail on the left switches between four
screens, and the two camera screens share the same live feed.

| Screen | What it does |
| --- | --- |
| **Detect** | **Capture** takes the frame on screen and reports whether the person is verified or anonymous. **Retake** releases the frame and resumes the feed. |
| **Add user** | **Save photo** writes the frame on screen to `CapturedImages/` as a registered user image. |
| **Images** | Every registered image, with a **Remove** button on each. |
| **Settings** | Turns the email alert on and points it at a mail server. |

Use the camera dropdown in the top right to pick a different capture device if
more than one is connected. It is hidden on the Images screen, where the camera
is stopped.

## How it works

Face recognition runs in-process using [FaceAiSharp](https://github.com/georg-jung/FaceAiSharp),
which wraps two ONNX models: SCRFD for face detection with landmarks, and
ArcFace for generating embeddings. SCRFD is licensed for non-commercial
research only, which limits what the app as a whole may be used for; see
[Licence](#licence).

[`FaceRecognizer`](PCUserDetection/FaceRecognizer.cs) does the work:

1. The captured frame is written to `AnonymousImages/Anonymous.jpeg`.
2. A face is detected in that frame, aligned using its landmarks, and turned
   into an embedding.
3. Each `.jpeg` in `CapturedImages/` gets the same treatment.
4. The dot product of the two embeddings is their cosine similarity. Anything
   at or above **0.42** counts as the same person.

An image with no detectable face is skipped rather than failing the whole
comparison. The models are loaded once and reused for every detection, since
loading them takes roughly a second.

## Email alerts

When a capture on **Detect** comes back anonymous, the app can email you the
frame that failed the check. It is off until you turn it on, and the app ships
with no server, address or key of any kind in it.

Mail goes out over plain SMTP through
[MailKit](https://github.com/jstedfast/MailKit), so any mailbox you already own
will do — Gmail, a company relay, a local test server. There is no account to
sign up for and no provider baked into the code.

### Setting it up

Fill in the **Settings** screen and press **Save**. **Send test email** proves
the settings work before you rely on them; whatever the server says comes back
on the line underneath.

| Setting | What it is for |
| --- | --- |
| **Email an alert** | Off means nothing is ever sent, whatever else is filled in. |
| **Attach the photo** | Attaches the captured frame to the message. |
| **Cooldown** | Minutes before another alert may go out. Without it, somebody sitting in front of the camera is a mailbox full of alerts. |
| **From** / **To** | The mailboxes it is sent from and to. **To** takes several addresses separated by commas. |
| **Send using** | **SMTP server**, or **Folder** to write the message to disk instead. |
| **Server** / **Port** / **Security** | Where to connect, and how. STARTTLS on port 587 is the usual pair; SSL is the older scheme on port 465. |
| **Username** / **Password** | Left empty for a relay that does not ask who is connecting. |

For Gmail, the server is `smtp.gmail.com` on port 587 with STARTTLS, and the
password is a 16-character [App Password](https://support.google.com/accounts/answer/185833)
rather than the password you sign in with. Google needs two-step verification
on the account before it will issue one. Outlook.com and Microsoft 365 no
longer accept a password over SMTP at all, so send from somewhere else.

**Folder** writes the message to `%APPDATA%\PCUserDetection\SentMail\` as an
`.eml` file instead of sending it. Every part of the alert runs — the check,
the cooldown, the attachment, the composed message — with nothing but a folder
to look in afterwards, which is what a fresh clone with no mail account needs.

### Where the settings are kept

In `%APPDATA%\PCUserDetection\email.json`, next to the theme setting, and never
in the repository. [`email.example.json`](email.example.json) shows the shape
of the file; there is no need to write it by hand.

The password is not stored in the clear. It is encrypted with DPAPI under the
Windows account that entered it, which means the file cannot be read on another
machine or by another user, and the app has no key of its own to keep anywhere.
Copy the file to a second PC and the password comes back empty, so type it in
again there.

## Project layout

```
PCUserDetection/
├── UserFaceDetector.cs      the window: navigation and the four screens
├── Theme.cs                 colours, fonts and button styles
├── CameraView.cs            the webcam feed and the frame it hands out
├── ImageCard.cs             one registered image in the gallery
├── ChoiceStrip.cs           a row of buttons where one is the choice in force
├── SettingsPanel.cs         the Settings screen
├── FaceRecognizer.cs        face detection and embedding comparison
├── EmailAlert.cs            whether a failed check becomes an email, and what it says
├── EmailSender.cs           the transports: SMTP, and the folder used in its place
├── EmailSettings.cs         the alert settings, and the encrypted password
├── AppPaths.cs              resolves the image folders and the settings files
├── AnonymousImages/         the most recent captured frame
└── CapturedImages/          registered user images
```

Image paths are resolved in one place, [`AppPaths`](PCUserDetection/AppPaths.cs),
from the folder the executable lives in. Running from the source tree, it walks
up to the folder holding `PCUserDetection.csproj` and keeps the two folders
there, so a rebuild does not lose the images already registered. A published
build has no project folder above it, so the images go under
`%APPDATA%\PCUserDetection` instead, which is somewhere the person running the
app can always write. Both folders are created on demand if they are missing.
The settings files live under `%APPDATA%` in either case.

The alert is split so that the decision and the delivery stay apart:
[`EmailAlert`](PCUserDetection/EmailAlert.cs) decides whether to send and writes
the message, and an `IEmailSender` in
[`EmailSender.cs`](PCUserDetection/EmailSender.cs) delivers it. Sending through
a provider's HTTP API instead would be another implementation of that
interface, with nothing above it changing.

## Notes

- The look is defined in one file, [`Theme.cs`](PCUserDetection/Theme.cs).
  Editing the colours there changes the whole app; nothing else hardcodes one.
- There is a light and a dark palette, switched from **Appearance** at the
  bottom of the rail: **Light**, **Dark**, or **Auto** to follow the app theme
  Windows is set to. The window re-paints straight away, and the choice is
  remembered in `%APPDATA%\PCUserDetection\theme.txt` for the next run.
- Switching works because the designer only lays the window out. Every colour
  is applied by `ApplyTheme` in
  [`UserFaceDetector.cs`](PCUserDetection/UserFaceDetector.cs), so re-theming
  is a matter of choosing a palette and calling it again. A new control needs
  its colours set there, not in the designer.
- **Auto** reads the Windows setting when it is chosen and when the app starts.
  Changing Windows between light and dark while the app is open does not
  re-paint it; the next run picks the new setting up.
- The screens are docked rather than positioned by pixel, so the window can be
  resized and the camera feed grows with it.
- The Settings screen uses [`ChoiceStrip`](PCUserDetection/ChoiceStrip.cs) and
  bordered panels rather than combo boxes, check boxes and bordered text boxes.
  Those three paint parts of themselves in system colours that no property
  turns off, which looks wrong against the dark palette; a button and a panel
  obey the colours they are given.
- A failed alert never interrupts anything. It comes back as a result that ends
  up on the status line, so an unreachable mail server costs you the alert and
  nothing else.
- Recognition used to live in a separate `FaceDetection` console app that the
  form launched as a child process and parsed stdout from. It now runs
  in-process, which removed the hardcoded path to the console executable and
  avoids reloading the ONNX models on every capture.
- A running instance locks `bin\Debug\PCUserDetection.exe`, so close the app
  before rebuilding.

## Licence

The code in this repository is [MIT licensed](LICENSE). Do what you like with
it, as long as the copyright notice comes along.

> **Not for commercial use as it stands.** The face detection model this app
> depends on is licensed for non-commercial research only. That restriction
> comes from the model rather than from this code, and it applies no matter
> what the repository itself is licensed under.

The dependencies are not all as permissive as the code that uses them:

| Dependency | Licence | What it means for you |
| --- | --- | --- |
| **SCRFD model**, via `FaceAiSharp.Bundle` | Non-commercial research only | The face detector. [Its licence](https://github.com/deepinsight/insightface/tree/master/python-package#model-zoo) is the restriction above. Swapping the detector for a permissively licensed model is the way out of it. |
| **ImageSharp** | Six Labors Split | Apache 2.0 for open source projects and for organisations under 1M USD annual gross revenue. A paid licence for everyone else. |
| **AForge.Video.DirectShow** | LGPL v3 | The webcam capture. Referenced as a NuGet package and never modified, which is what LGPL asks for. Keep the notice, and leave the assembly replaceable. |
| **MailKit**, **MimeKit** | MIT | No conditions beyond the notice. |
| **FaceAiSharp**, **ArcFace model**, **ONNX Runtime**, **ProtectedData** | MIT / Apache 2.0 | No conditions beyond the notice. |

Each package's full licence text ships with it and can be read in the local
NuGet cache under `%USERPROFILE%\.nuget\packages\`.
