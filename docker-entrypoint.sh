#!/usr/bin/env bash

# Copy core plugins if needed
if [[ -z "$HIVE_SKIP_COPY_CORE_PLUGINS" ]]; then
  cp -R -u -p "/app/core-plugins/." "/app/plugins/"
fi

exec "$@"
