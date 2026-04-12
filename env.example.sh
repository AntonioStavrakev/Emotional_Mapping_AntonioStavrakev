#!/usr/bin/env zsh

# API
export ConnectionStrings__Default='Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true'
export OpenAI__ApiKey='YOUR_OPENAI_API_KEY'
export OpenAI__Model='gpt-4.1-mini'

# External place discovery
# Recommended mode: Google Places with OSM fallback
export ExternalPlaces__Provider='google'
export GooglePlaces__ApiKey='YOUR_GOOGLE_PLACES_API_KEY'

# Stripe
export Stripe__SecretKey='YOUR_STRIPE_SECRET_KEY'
export Stripe__PublishableKey='YOUR_STRIPE_PUBLISHABLE_KEY'
export Stripe__WebhookSecret='YOUR_STRIPE_WEBHOOK_SECRET'

# Web
export Email__Username='YOUR_EMAIL_USERNAME'
export Email__Password='YOUR_EMAIL_APP_PASSWORD'
export Email__To='YOUR_EMAIL_TO'
export Authentication__Google__ClientId='YOUR_GOOGLE_AUTH_CLIENT_ID'
export Authentication__Google__ClientSecret='YOUR_GOOGLE_AUTH_CLIENT_SECRET'

# Optional alternative provider
# export FoursquarePlaces__ApiKey='YOUR_FOURSQUARE_API_KEY'
