let fieldData;
let currentClipIndex = 0;
let clips = [];

window.addEventListener('onWidgetLoad', function (obj) {
    console.log('Widget loaded');
    fieldData = obj.detail.fieldData;
    console.log('Field data:', fieldData);
    playClips();

    // Add event listeners for date changes
    document.querySelectorAll('input[type="date"]').forEach(input => {
        input.addEventListener('change', () => {
            console.log('Date changed:', input.name, input.value);
            fieldData[input.name] = input.value;
            playClips();
        });
    });
});

async function fetchClips(startDate, endDate) {
    try {
        console.log('Fetching clips from', startDate, 'to', endDate);
        // Ensure dates are in ISO 8601 format
        const formattedStartDate = new Date(startDate).toISOString();
        const formattedEndDate = new Date(endDate).toISOString();

        const response = await fetch(`http://localhost:5069/clips?start_date=${formattedStartDate}&end_date=${formattedEndDate}`);
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to fetch clips: ${response.statusText} - ${errorText}`);
        }

        const fetchedClips = await response.json();
        console.log('Fetched clips:', fetchedClips);
        return fetchedClips;
    } catch (error) {
        updateDebugMessages(`Error fetching clips: ${error.message}`);
        return [];
    }
}

async function playClips() {
    try {
        let startDate = fieldData['startDate'];
        let endDate = fieldData['endDate'];

        // Validate and format dates
        if (!startDate || isNaN(new Date(startDate).getTime())) {
            throw new Error('Invalid start date');
        }
        if (!endDate || isNaN(new Date(endDate).getTime())) {
            throw new Error('Invalid end date');
        }

        clips = await fetchClips(startDate, endDate);
        const clipPlayer = document.getElementById('clip-player');

        if (!clipPlayer) {
            updateDebugMessages('No element with ID "clip-player" found');
            return;
        }

        if (clips.length === 0) {
            clipPlayer.innerText = 'No clips available';
            return;
        }

        currentClipIndex = 0; // Reset the clip index

        function playNextClip() {
            if (currentClipIndex >= clips.length) {
                currentClipIndex = 0;
                // Fetch new clips with a random date range
                const { randomStartDate, randomEndDate } = generateRandomDateRange();
                fetchClips(randomStartDate, randomEndDate).then(newClips => {
                    clips = newClips;
                    playNextClip();
                });
                return;
            }

            const clip = clips[currentClipIndex];
            updateDebugMessages(`Playing clip: ${clip.title}`);
            clipPlayer.src = clip.thumbnail_url.replace('-preview-480x272.jpg', '.mp4');
            clipPlayer.play();
            currentClipIndex++;
        }

        clipPlayer.removeEventListener('ended', playNextClip); // Remove any existing event listener
        clipPlayer.addEventListener('ended', playNextClip);
        playNextClip();
    } catch (error) {
        updateDebugMessages(`Error: ${error.message}`);
    }
}

function generateRandomDateRange() {
    const start = new Date(2020, 0, 1); // Start date (e.g., January 1, 2020)
    const end = new Date(); // End date (current date)
    const randomStartDate = new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
    const randomEndDate = new Date(randomStartDate.getTime() + Math.random() * (end.getTime() - randomStartDate.getTime()));
    return {
        randomStartDate: randomStartDate.toISOString(),
        randomEndDate: randomEndDate.toISOString()
    };
}

function updateDebugMessages(message) {
    console.log(message);
    const debugMessages = document.getElementById('debug-messages');
    debugMessages.innerText = message;
}