// Olive confetti. Draws onto a throwaway full screen canvas so nothing in the
// Blazor render tree has to know about it.
(function () {
    const OLIVE_GREENS = ['#6b8e23', '#808000', '#556b2f', '#8faa3c', '#4f6b1f'];
    const PIMENTO = '#c8412c';
    const FROST_BLUES = ['#dff3ff', '#b6e3f7', '#8ecfee', '#ffffff', '#cfe9fb'];

    let canvas = null;
    let context = null;
    let particles = [];
    let animationHandle = 0;

    function prefersReducedMotion() {
        return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    function ensureCanvas() {
        if (canvas) {
            return;
        }

        canvas = document.createElement('canvas');
        canvas.style.position = 'fixed';
        canvas.style.inset = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.pointerEvents = 'none';
        canvas.style.zIndex = '2000';
        document.body.appendChild(canvas);
        context = canvas.getContext('2d');

        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);
    }

    function resizeCanvas() {
        if (!canvas) {
            return;
        }

        const ratio = window.devicePixelRatio || 1;
        canvas.width = window.innerWidth * ratio;
        canvas.height = window.innerHeight * ratio;
        context.setTransform(ratio, 0, 0, ratio, 0, 0);
    }

    function teardown() {
        window.removeEventListener('resize', resizeCanvas);
        if (canvas && canvas.parentNode) {
            canvas.parentNode.removeChild(canvas);
        }
        canvas = null;
        context = null;
        particles = [];
        animationHandle = 0;
    }

    function spawnBurst(originX, originY, count, power) {
        for (let i = 0; i < count; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = power * (0.35 + Math.random() * 0.65);

            particles.push({
                kind: 'olive',
                x: originX,
                y: originY,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed - power * 0.35,
                radius: 4 + Math.random() * 5,
                rotation: Math.random() * Math.PI,
                spin: (Math.random() - 0.5) * 0.25,
                gravity: 0.22,
                drag: 0.99,
                life: 1,
                decay: 0.006 + Math.random() * 0.008,
                color: OLIVE_GREENS[Math.floor(Math.random() * OLIVE_GREENS.length)]
            });
        }
    }

    function spawnFrostBurst(originX, originY, count, power) {
        for (let i = 0; i < count; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = power * (0.3 + Math.random() * 0.7);

            particles.push({
                kind: 'flake',
                x: originX,
                y: originY,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                radius: 3 + Math.random() * 5,
                rotation: Math.random() * Math.PI,
                spin: (Math.random() - 0.5) * 0.12,
                // Flakes hang in the air and settle slowly instead of dropping like fruit.
                gravity: 0.035,
                drag: 0.94,
                life: 1,
                decay: 0.012 + Math.random() * 0.012,
                color: FROST_BLUES[Math.floor(Math.random() * FROST_BLUES.length)]
            });
        }
    }

    function drawOlive(particle) {
        context.save();
        context.globalAlpha = Math.max(particle.life, 0);
        context.translate(particle.x, particle.y);
        context.rotate(particle.rotation);

        context.fillStyle = particle.color;
        context.beginPath();
        context.ellipse(0, 0, particle.radius * 0.72, particle.radius, 0, 0, Math.PI * 2);
        context.fill();

        // The pimento gives each particle its unmistakable dirty martini look.
        context.fillStyle = PIMENTO;
        context.beginPath();
        context.ellipse(0, particle.radius * 0.45, particle.radius * 0.26, particle.radius * 0.32, 0, 0, Math.PI * 2);
        context.fill();

        context.restore();
    }

    function drawFlake(particle) {
        context.save();
        context.globalAlpha = Math.max(particle.life, 0);
        context.translate(particle.x, particle.y);
        context.rotate(particle.rotation);

        context.strokeStyle = particle.color;
        context.lineWidth = Math.max(particle.radius * 0.18, 0.8);
        context.lineCap = 'round';

        // Six spokes, each with a pair of small barbs, reads as a snowflake.
        for (let i = 0; i < 6; i++) {
            context.rotate(Math.PI / 3);

            context.beginPath();
            context.moveTo(0, 0);
            context.lineTo(0, -particle.radius);
            context.stroke();

            context.beginPath();
            context.moveTo(0, -particle.radius * 0.6);
            context.lineTo(particle.radius * 0.3, -particle.radius * 0.85);
            context.moveTo(0, -particle.radius * 0.6);
            context.lineTo(-particle.radius * 0.3, -particle.radius * 0.85);
            context.stroke();
        }

        context.restore();
    }

    function tick() {
        context.clearRect(0, 0, window.innerWidth, window.innerHeight);

        for (const particle of particles) {
            particle.vy += particle.gravity;
            particle.vx *= particle.drag;
            particle.vy *= particle.drag;
            particle.x += particle.vx;
            particle.y += particle.vy;
            particle.rotation += particle.spin;
            particle.life -= particle.decay;

            if (particle.kind === 'flake') {
                drawFlake(particle);
            } else {
                drawOlive(particle);
            }
        }

        particles = particles.filter(p => p.life > 0 && p.y < window.innerHeight + 60);

        if (particles.length === 0) {
            teardown();
            return;
        }

        animationHandle = requestAnimationFrame(tick);
    }

    /**
     * Snowflake firework centred on an element, used when ice crispys are ticked.
     * The canvas sits above page content, so this works from inside the modal.
     */
    window.celebrateIceCrispys = function (element) {
        if (prefersReducedMotion() || !element) {
            return;
        }

        const bounds = element.getBoundingClientRect();
        const x = bounds.left + bounds.width / 2;
        const y = bounds.top + bounds.height / 2;

        ensureCanvas();
        spawnFrostBurst(x, y, 30, 6);

        // A smaller delayed puff makes it sparkle rather than just pop.
        window.setTimeout(() => {
            ensureCanvas();
            spawnFrostBurst(x, y, 18, 3.5);

            if (!animationHandle) {
                animationHandle = requestAnimationFrame(tick);
            }
        }, 160);

        if (!animationHandle) {
            animationHandle = requestAnimationFrame(tick);
        }
    };

    /**
     * Celebrates a saved rating with one burst per whole olive scored, so a 7.5
     * fires seven bursts. They are staggered so it reads as fireworks.
     */
    window.celebrateRating = function (score) {
        if (prefersReducedMotion()) {
            return;
        }

        const burstCount = Math.min(Math.max(Math.floor(Number(score) || 0), 1), 10);

        for (let i = 0; i < burstCount; i++) {
            const fire = () => {
                // The canvas tears itself down when it empties, so rebuild if a
                // later burst lands after a lull.
                ensureCanvas();

                const width = window.innerWidth;
                const height = window.innerHeight;

                // The first burst is centred, the rest scatter across the screen.
                const x = i === 0 ? width / 2 : width * (0.15 + Math.random() * 0.7);
                const y = i === 0 ? height * 0.45 : height * (0.2 + Math.random() * 0.4);

                spawnBurst(x, y, 34, 7 + Math.random() * 3);

                if (!animationHandle) {
                    animationHandle = requestAnimationFrame(tick);
                }
            };

            if (i === 0) {
                fire();
            } else {
                window.setTimeout(fire, 180 * i);
            }
        }
    };
})();
