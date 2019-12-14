counter=0; 
function header() {
  startAt=${startAt:-$(date +%s)}; elapsed=$(date +%s); elapsed=$((elapsed-startAt));
  if [[ $(uname -s) != Darwin ]]; then
     elapsed=$(TZ=UTC date -d "@${elapsed}" "+%_H:%M:%S" 2>/dev/null);
  else 
     elapsed=$(TZ=UTC date -r "${elapsed}" "+%_H:%M:%S" 2>/dev/null);
  fi
  LightGreen='\033[1;32m'; Yellow='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'; LightGray='\033[1;2m';
  printf "${LightGray}${elapsed:-}${NC} ${LightGreen}$1${NC} ${Yellow}$2${NC}\n"; 
}
function Say() { 
    echo ""; 
    counter=$((counter+1)); 
    header "STEP $counter" "$1"; 
}
Say "" >/dev/null; 
counter=0; 
