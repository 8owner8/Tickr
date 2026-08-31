#!/usr/bin/env sh
set -eu

CONFIG_PATH="config/TickrApp.json"
OS_TYPE="$(uname -s)"

case "$OS_TYPE" in
	"Darwin") SCRIPT_PATH="$(readlink "$0")" ;;
	"FreeBSD") SCRIPT_PATH="$(readlink -f "$0")" ;;
	"Linux") SCRIPT_PATH="$(readlink -f "$0")" ;;
	*) echo "ERROR: Unknown OS type: ${OS_TYPE}. If you believe that our script should work on your machine, please let us know."; exit 1
esac

SCRIPT_DIR="$(dirname "$SCRIPT_PATH")"
BINARY="${SCRIPT_DIR}/Tickr.dll"

if [ ! -f "$BINARY" ]; then
	echo "ERROR: $BINARY could not be found!"
	exit 1
fi

cd "$SCRIPT_DIR"

PATH_NEXT=0
SERVICE=0

PARSE_ARG() {
	case "$1" in
		--path) PATH_NEXT=1 ;;
		--path=*)
			if [ "$PATH_NEXT" -eq 1 ]; then
				PATH_NEXT=0
				cd "$1"
			else
				cd "$(echo "$1" | cut -d '=' -f 2-)"
			fi
			;;
		--service) SERVICE=1 ;;
		*)
			if [ "$PATH_NEXT" -eq 1 ]; then
				PATH_NEXT=0
				cd "$1"
			fi
	esac
}

if [ -n "${Tickr_PATH-}" ]; then
	cd "$Tickr_PATH"
fi

if [ -n "${Tickr_ARGS-}" ]; then
	for ARG in $Tickr_ARGS; do
		if [ -n "$ARG" ]; then
			PARSE_ARG "$ARG"
		fi
	done
fi

for ARG in "$@"; do
	if [ -n "$ARG" ]; then
		PARSE_ARG "$ARG"
	fi
done

BINARY_PREFIX=""

if [ -n "${Tickr_UID-}" ] && [ "$(id -u)" -eq 0 ] && id -u "$Tickr_UID" >/dev/null 2>&1 && [ "$(id -u "$Tickr_UID")" -gt 0 ]; then
	# Fix permissions first to ensure Tickr has read/write access to the directory specified by --path and its own
	chown -hR "${Tickr_UID}:${Tickr_UID}" . "$SCRIPT_DIR" || true

	BINARY_PREFIX="su $(id -nu "$Tickr_UID") -c"
fi

CONFIG_PATH="$(pwd)/${CONFIG_PATH}"

# Kill underlying Tickr process on shell process exit
trap "trap - TERM && kill -- -$$" INT TERM

if ! command -v dotnet >/dev/null; then
	echo "ERROR: dotnet CLI tools are not installed!"
	exit 1
fi

dotnet --info

if [ "$SERVICE" -eq 1 ] || ([ -f "$CONFIG_PATH" ] && grep -Eq '"Headless":\s+?true' "$CONFIG_PATH"); then
	# We're running Tickr in headless mode so we don't need STDIN
	# Start Tickr in the background, trap will work properly due to non-blocking call
	if [ -n "$BINARY_PREFIX" ]; then
		$BINARY_PREFIX "dotnet ${DOTNET_ARGS-} $BINARY $*" &
	else
		dotnet ${DOTNET_ARGS-} "$BINARY" "$@" &
	fi

	# This will forward dotnet error code, set -e will abort the script if it's non-zero
	wait $!
else
	# We're running Tickr in non-headless mode, so we need STDIN to be operative
	# Start Tickr in the foreground, trap won't work until process exit
	if [ -n "$BINARY_PREFIX" ]; then
		$BINARY_PREFIX "dotnet ${DOTNET_ARGS-} $BINARY $*"
	else
		dotnet ${DOTNET_ARGS-} "$BINARY" "$@"
	fi
fi
