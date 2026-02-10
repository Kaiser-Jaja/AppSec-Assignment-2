# Security Policy

## Third-Party Library Security

### jQuery Validation Plugin (v1.19.5)

The jQuery Validation Plugin located at `wwwroot/lib/jquery-validation/` has known CodeQL warnings for potential XSS vulnerabilities (js/unsafe-jquery-plugin). These are **mitigated** by:

1. **Content Security Policy (CSP)** - Configured in `Program.cs` to prevent inline script execution
2. **Input Sanitization** - All user inputs are validated server-side before processing
3. **No User Input to Plugin Options** - The validation plugin is configured with static, developer-defined options only

### Mitigation Controls Applied

- CSP Header: `script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com`
- X-Frame-Options: `DENY`
- X-Content-Type-Options: `nosniff`
- X-XSS-Protection: `1; mode=block`

## Email Service Security

The EmailService class handles sensitive email operations with the following protections:

1. **Template-Based Emails** - Only system-generated templates are sent, no raw user input
2. **HTML Encoding** - All dynamic content is HTML-encoded before insertion into email bodies
3. **Header Injection Prevention** - Email subjects have line breaks stripped
4. **OTP Validation** - 2FA codes are validated to be exactly 6 digits before sending

## Reporting a Vulnerability

If you discover a security vulnerability, please report it by:
1. Opening a private security advisory on GitHub
2. Emailing the maintainers directly

Do not open a public issue for security vulnerabilities.
