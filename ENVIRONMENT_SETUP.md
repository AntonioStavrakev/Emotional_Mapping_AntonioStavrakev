**Local Setup**

Use environment variables instead of storing secrets in `appsettings.json`.

For `zsh` on macOS:

```zsh
cd "/Users/antoniostavrakev/Documents/C#/Diploma Project 2026/Emotional_Mapping_AntonioStavrakev 2 copy"
source ./env.example.sh
```

Then replace the placeholder values in [env.example.sh](/Users/antoniostavrakev/Documents/C#/Diploma%20Project%202026/Emotional_Mapping_AntonioStavrakev%202%20copy/env.example.sh).

Important variables:

```zsh
ConnectionStrings__Default
OpenAI__ApiKey
ExternalPlaces__Provider
Stripe__SecretKey
Stripe__PublishableKey
Stripe__WebhookSecret
Email__Username
Email__Password
Email__To
Authentication__Google__ClientId
Authentication__Google__ClientSecret
```

Provider selection:

```zsh
export ExternalPlaces__Provider='google'
export GooglePlaces__ApiKey='YOUR_GOOGLE_PLACES_API_KEY'
```

Recommended default mode:

1. `google`
2. `osm` fallback if Google Places has no key or no results

Free-only alternative:

```zsh
export ExternalPlaces__Provider='osm'
```

or:

```zsh
export ExternalPlaces__Provider='foursquare'
export FoursquarePlaces__ApiKey='YOUR_FOURSQUARE_API_KEY'
```

Fallback order in code:

If `Provider='google'`:
1. `google`
2. `foursquare`
3. `osm`

If `Provider='foursquare'`:
1. `foursquare`
2. `google`
3. `osm`
