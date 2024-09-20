let fieldData;
let currentClipIndex = 0;
let clips = [];
let broadcasterName = ''; // Declare broadcasterName here

window.addEventListener('onWidgetLoad', async function (obj) { // Make this function async
    fieldData = obj.detail.fieldData;

    try {
        const apiToken = obj.detail.channel.apiToken; // Assuming the API token is available here
        broadcasterName = await getBroadcasterName(apiToken);
    } catch (error) {
        updateDebugMessages(`Error fetching broadcaster name: ${error.message}`);
    }
    playClips();

    // Add event listeners for date changes
    document.querySelectorAll('input[type="date"]').forEach(input => {
        input.addEventListener('change', () => {
            fieldData[input.name] = input.value;
            playClips();

            const debugMessages = document.getElementById('debug-messages');
            debugMessages.textContent = fieldData.titleText;
            debugMessages.style.color = fieldData.titleColor;
            debugMessages.style.fontSize = `${fieldData.titleSize}px`;
            debugMessages.style.fontFamily = fieldData.fontName;
            debugMessages.style.fontWeight = fieldData.fontWeight;

            // Update the Google Font link
            const googleFontLink = document.getElementById('google-font-link');
            googleFontLink.href = `https://fonts.googleapis.com/css?family=${fieldData.fontName}:400,${fieldData.fontWeight}`;
        });
    });

    // Set initial visibility of debug messages based on the checkbox value
    const debugMessages = document.getElementById('debug-messages');
    debugMessages.style.display = fieldData.showDebugMessages ? 'block' : 'none';
});

async function getBroadcasterName(apiToken) {
    const response = await fetch("https://api.streamelements.com/kappa/v2/channels/me", {
        headers: {
            "accept": "application/json",
            "authorization": `apikey ${apiToken}`
        }
    });

    if (!response.ok) {
        throw new Error(`Error fetching broadcaster name: ${response.statusText}`);
    }

    const data = await response.json();
    return data.username;
}

async function fetchClips(startDate, endDate, broadcasterName) {
    try {

        // Ensure dates are in ISO 8601 format
        const formattedStartDate = new Date(startDate).toISOString();
        const formattedEndDate = new Date(endDate).toISOString();

        const response = await fetch(`https://twitchclipplayer-h3bcanc6bmdkawhz.northeurope-01.azurewebsites.net/clips?broadcaster_name=${broadcasterName}&start_date=${formattedStartDate}&end_date=${formattedEndDate}`);
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to fetch clips: ${response.statusText} - ${errorText}`);
        }

        const fetchedClips = await response.json();
        return fetchedClips;
    } catch (error) {
        updateDebugMessages(`Error fetching clips for broadcasterName ${broadcasterName}: ${error.message}`);
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

        clips = await fetchClips(startDate, endDate, broadcasterName);
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
                fetchClips(randomStartDate, randomEndDate, broadcasterName).then(newClips => {
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
    const start = new Date(2022, 3, 11); // Start date (e.g., January 1, 2020)
    const end = new Date(); // End date (current date)
    const randomStartDate = new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
    const randomEndDate = new Date(randomStartDate.getTime() + Math.random() * (end.getTime() - randomStartDate.getTime()));
    return {
        randomStartDate: randomStartDate.toISOString(),
        randomEndDate: randomEndDate.toISOString()
    };
}

function updateDebugMessages(message) {
    const debugMessages = document.getElementById('debug-messages');
    debugMessages.innerText = message;
    // Apply the text settings to the debug-messages div
    debugMessages.style.color = fieldData.titleColor;
    debugMessages.style.fontSize = `${fieldData.titleSize}px`;
    debugMessages.style.fontFamily = fieldData.fontName;
    debugMessages.style.fontWeight = fieldData.fontWeight;
    debugMessages.style.backgroundColor = fieldData.backgroundColor;
    debugMessages.style.display = fieldData.showDebugMessages ? 'block' : 'none';
}