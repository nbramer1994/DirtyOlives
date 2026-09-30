// A physics driven olive that falls down the page, bounces off real text, and
// rolls along whatever it lands on before tipping off the edge. Positions are
// read from the live layout, so it interacts with whatever is on screen.
(function () {
    const GRAVITY = 0.95;
    const BOUNCE = 0.6;           // vertical energy kept per bounce
    const FRICTION = 0.993;       // horizontal damping while airborne
    const ROLL_FRICTION = 0.998;  // damping while resting on a surface
    const MIN_BOUNCE_SPEED = 2.6; // below this it settles instead of bouncing
    const MIN_ROLL_SPEED = 1.6;   // a resting olive never crawls slower than this
    const SIZE = 34;
    const RADIUS = SIZE / 2;

    // Elements worth landing on. Anything too small or invisible is skipped.
    const LEDGE_SELECTOR = 'h1, h2, h3, h4, h5, p, li, .card, .alert, label, .badge, strong, td, th';

    let element = null;
    let ledges = [];
    let ledgeTimer = 0;
    let frame = 0;
    let running = false;

    const olive = {
        x: 0,
        y: 0,
        vx: 0,
        vy: 0,
        rotation: 0,
        resting: false
    };

    function prefersReducedMotion() {
        return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    function collectLedges() {
        ledges = [];

        for (const node of document.querySelectorAll(LEDGE_SELECTOR)) {
            const rect = node.getBoundingClientRect();

            // Only surfaces currently on screen and wide enough to land on.
            if (rect.width < 40 || rect.height < 8) {
                continue;
            }

            if (rect.bottom < 0 || rect.top > window.innerHeight) {
                continue;
            }

            ledges.push({ left: rect.left, right: rect.right, top: rect.top });
        }

        // Nearest surfaces first so the olive lands on the topmost thing below it.
        ledges.sort((a, b) => a.top - b.top);
    }

    function reset() {
        olive.x = window.innerWidth * (0.15 + Math.random() * 0.7);
        olive.y = -SIZE;
        olive.vx = (Math.random() - 0.5) * 7;
        // Enters already moving rather than easing in from a standstill.
        olive.vy = 6;
        olive.rotation = Math.random() * Math.PI * 2;
        olive.resting = false;
    }

    /** The surface directly beneath the olive, if any. */
    function surfaceBelow(x, fromY, toY) {
        let best = null;

        for (const ledge of ledges) {
            if (x < ledge.left || x > ledge.right) {
                continue;
            }

            const surface = ledge.top - RADIUS;

            // Only surfaces the olive is crossing on this step.
            if (surface < fromY - 1 || surface > toY) {
                continue;
            }

            if (best === null || surface < best) {
                best = surface;
            }
        }

        return best;
    }

    /** True when the olive still has something under it to rest on. */
    function isSupported(x, y) {
        for (const ledge of ledges) {
            if (x < ledge.left || x > ledge.right) {
                continue;
            }

            if (Math.abs(ledge.top - RADIUS - y) < 2) {
                return true;
            }
        }

        return false;
    }

    function step() {
        const previousY = olive.y;

        if (olive.resting) {
            // Rolling along a surface until it runs out of ledge.
            olive.vx *= ROLL_FRICTION;

            // Always keep it moving so it never parks on a wide element.
            if (Math.abs(olive.vx) < MIN_ROLL_SPEED) {
                olive.vx = olive.vx < 0 ? -MIN_ROLL_SPEED : MIN_ROLL_SPEED;
            }

            olive.x += olive.vx;

            if (!isSupported(olive.x, olive.y)) {
                olive.resting = false;
                olive.vy = 1.5;
            }
        } else {
            olive.vy += GRAVITY;
            olive.vx *= FRICTION;
            olive.x += olive.vx;
            olive.y += olive.vy;

            const landing = surfaceBelow(olive.x, previousY, olive.y);

            if (landing !== null && olive.vy > 0) {
                olive.y = landing;

                if (olive.vy > MIN_BOUNCE_SPEED) {
                    // Bounce, with a nudge sideways so it walks across the page.
                    olive.vy = -olive.vy * BOUNCE;
                    olive.vx += (Math.random() - 0.5) * 4;
                } else {
                    olive.vy = 0;
                    olive.resting = true;
                }
            }
        }

        // Rotation follows horizontal travel so it reads as rolling.
        olive.rotation += olive.vx / RADIUS;

        // Bounce off the side walls rather than disappearing.
        if (olive.x < RADIUS) {
            olive.x = RADIUS;
            olive.vx = Math.abs(olive.vx) * 0.7;
        } else if (olive.x > window.innerWidth - RADIUS) {
            olive.x = window.innerWidth - RADIUS;
            olive.vx = -Math.abs(olive.vx) * 0.7;
        }

        // Off the bottom: send a fresh one in from the top.
        if (olive.y - RADIUS > window.innerHeight) {
            window.setTimeout(reset, 400 + Math.random() * 900);
            olive.y = window.innerHeight + SIZE * 4; // park it off screen meanwhile
        }

        element.style.transform =
            `translate3d(${olive.x - RADIUS}px, ${olive.y - RADIUS}px, 0) rotate(${olive.rotation}rad)`;

        frame = requestAnimationFrame(step);
    }

    window.startRollingOlive = function (el) {
        if (!el || running || prefersReducedMotion()) {
            return;
        }

        running = true;
        element = el;

        collectLedges();
        // Layout shifts as content renders, so refresh the surfaces periodically.
        ledgeTimer = window.setInterval(collectLedges, 1000);
        window.addEventListener('resize', collectLedges);

        reset();
        frame = requestAnimationFrame(step);
    };

    window.stopRollingOlive = function () {
        running = false;
        cancelAnimationFrame(frame);
        window.clearInterval(ledgeTimer);
        window.removeEventListener('resize', collectLedges);
        element = null;
    };
})();
