// Physics driven olives that fall down the page, bounce off real text, and roll
// along whatever they land on. Each olive is created on demand and removes
// itself once it leaves the screen. Positions are read from the live layout, so
// they interact with whatever is currently rendered.
(function () {
	const GRAVITY = 0.95;
	const BOUNCE = 0.6;           // vertical energy kept per bounce
	const FRICTION = 0.993;       // horizontal damping while airborne
	const ROLL_FRICTION = 0.998;  // damping while resting on a surface
	const MIN_BOUNCE_SPEED = 2.6; // below this it settles instead of bouncing
	const MIN_ROLL_SPEED = 1.6;   // a resting olive never crawls slower than this
	const SIZE = 34;
	const RADIUS = SIZE / 2;
	const MAX_OLIVES = 12;        // keeps a mashed button from flooding the page

	// Elements worth landing on. Anything too small or invisible is skipped.
	const LEDGE_SELECTOR = 'h1, h2, h3, h4, h5, p, li, .card, .alert, label, .badge, strong, td, th';

	const SVG = `
		<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" focusable="false">
			<ellipse cx="12" cy="12" rx="9" ry="11" fill="#7a9a2b" />
			<path d="M12 1a9 11 0 0 1 0 22 9 11 0 0 0 0-22z" fill="#5d7a1e" opacity="0.55" />
			<ellipse cx="8.4" cy="7.6" rx="2" ry="3.2" fill="#c3dc72" opacity="0.7" transform="rotate(-20 8.4 7.6)" />
			<ellipse cx="12" cy="12" rx="3.1" ry="4.3" fill="#c0392b" />
			<ellipse cx="11.2" cy="10.6" rx="0.9" ry="1.3" fill="#e8705f" opacity="0.8" />
		</svg>`;

	let olives = [];
	let ledges = [];
	let ledgeTimer = 0;
	let frame = 0;

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

		// Nearest surfaces first so an olive lands on the topmost thing below it.
		ledges.sort((a, b) => a.top - b.top);
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

	function advance(olive) {
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

		olive.element.style.transform =
			`translate3d(${olive.x - RADIUS}px, ${olive.y - RADIUS}px, 0) rotate(${olive.rotation}rad)`;

		// Gone for good once it clears the bottom of the viewport.
		return olive.y - RADIUS <= window.innerHeight;
	}

	function step() {
		const survivors = [];

		for (const olive of olives) {
			if (advance(olive)) {
				survivors.push(olive);
			} else {
				olive.element.remove();
			}
		}

		olives = survivors;

		if (olives.length === 0) {
			stopLoop();
			return;
		}

		frame = requestAnimationFrame(step);
	}

	function startLoop() {
		if (frame) {
			return;
		}

		collectLedges();
		// Layout shifts as content renders, so refresh the surfaces periodically.
		ledgeTimer = window.setInterval(collectLedges, 1000);
		window.addEventListener('resize', collectLedges);

		frame = requestAnimationFrame(step);
	}

	function stopLoop() {
		cancelAnimationFrame(frame);
		window.clearInterval(ledgeTimer);
		window.removeEventListener('resize', collectLedges);
		frame = 0;
		ledgeTimer = 0;
	}

	/** Drops a single olive from the top of the page. */
	window.dropOlive = function () {
		if (prefersReducedMotion() || olives.length >= MAX_OLIVES) {
			return;
		}

		const element = document.createElement('div');
		element.className = 'rolling-olive';
		element.setAttribute('aria-hidden', 'true');
		element.innerHTML = SVG;
		document.body.appendChild(element);

		olives.push({
			element,
			x: window.innerWidth * (0.15 + Math.random() * 0.7),
			y: -SIZE,
			vx: (Math.random() - 0.5) * 7,
			// Enters already moving rather than easing in from a standstill.
			vy: 6,
			rotation: Math.random() * Math.PI * 2,
			resting: false
		});

		startLoop();
	};

	/** Removes every olive currently on screen. */
	window.clearOlives = function () {
		for (const olive of olives) {
			olive.element.remove();
		}

		olives = [];
		stopLoop();
	};
})();
