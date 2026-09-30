// Olive confetti. Draws onto a throwaway full screen canvas so nothing in the
// Blazor render tree has to know about it.
(function () {
    const OLIVE_GREENS = ['#6b8e23', '#808000', '#556b2f', '#8faa3c', '#4f6b1f'];
    const PIMENTO = '#c8412c';

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
                x: originX,
                y: originY,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed - power * 0.35,
                radius: 4 + Math.random() * 5,
                rotation: Math.random() * Math.PI,
                spin: (Math.random() - 0.5) * 0.25,
                life: 1,
                decay: 0.006 + Math.random() * 0.008,
                color: OLIVE_GREENS[Math.floor(Math.random() * OLIVE_GREENS.length)]
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

    function tick() {
        context.clearRect(0, 0, window.innerWidth, window.innerHeight);

        for (const particle of particles) {
            particle.vy += 0.22;      // gravity
            particle.vx *= 0.99;      // drag
            particle.vy *= 0.99;
            particle.x += particle.vx;
            particle.y += particle.vy;
            particle.rotation += particle.spin;
            particle.life -= particle.decay;

            drawOlive(particle);
        }

        particles = particles.filter(p => p.life > 0 && p.y < window.innerHeight + 60);

        if (particles.length === 0) {
            teardown();
            return;
        }

        animationHandle = requestAnimationFrame(tick);
    }

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
