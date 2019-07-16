#!/bin/bash

function assignNonEmptyValue {
    if [ ! -z "$1" ]
    then
        echo $1
    else
        echo $2
    fi
}

function splitAndGetArgs {
    finalstring=""
    readarray -d , -t strarr <<< "$2"
    for (( n=0; n < ${#strarr[*]}; n++))
    do
        finalstring=$finalstring"--$1 \"$(echo ${strarr[n]} | xargs)\" "
    done
    echo $finalstring
}

function showRunCommand {
    GREEN='\033[1;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m' # No Color

    echo -e "${GREEN}Run the following command to start Tentacle${NC}"
    echo -e "${YELLOW}sudo /opt/octopus/tentacle/Tentacle run --instance \"$1\" --noninteractive${NC}"
}

function setupListeningTentacle {
    instancename=$1
    logpath="/etc/octopus"
    applicationpath="/home/Octopus/Applications"
    port="10933"
    
    read -p "Where would you like Tentacle to store log files? ($logpath):" inputlogpath
    logpath=$(assignNonEmptyValue $inputlogpath $logpath)
    
    read -p "Where would you like Tentacle to install appications to? ($applicationpath):" inputapplicationpath
    applicationpath=$(assignNonEmptyValue $inputapplicationpath $applicationpath)

    read -p "Enter the port that this Tentacle will listen on ($port):" inputport
    port=$(assignNonEmptyValue $inputport $port)

    while [ -z "$thumbprint" ] 
    do
        read -p 'Enter the thumbprint of the Octopus Server: ' thumbprint
    done 

    GREEN='\033[1;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m' # No Color

    echo -e "${GREEN}The following configuration commands will be run to configure Tentacle:"

    echo -e "${YELLOW}sudo /opt/octopus/tentacle/Tentacle create-instance --instance \"$instancename\" --config \"$logpath/$instancename/tentacle-$instancename.config\""
    echo "sudo /opt/octopus/tentacle/Tentacle new-certificate --instance \"$instancename\" --if-blank"
    echo "sudo /opt/octopus/tentacle/Tentacle configure --instance \"$instancename\" --app \"$applicationpath\" --port $port --noListen False --reset-trust"
    echo -e "sudo /opt/octopus/tentacle/Tentacle configure --instance \"$instancename\" --trust $thumbprint${NC}"

    read -p "Press enter to continue..."
    
    sudo /opt/octopus/tentacle/Tentacle create-instance --instance "$instancename" --config "$logpath/$instancename/tentacle-$instancename.config"
    sudo /opt/octopus/tentacle/Tentacle new-certificate --instance "$instancename" --if-blank
    sudo /opt/octopus/tentacle/Tentacle configure --instance "$instancename" --app "$applicationpath" --port $port --noListen False --reset-trust
    sudo /opt/octopus/tentacle/Tentacle configure --instance "$instancename" --trust $thumbprint

    showRunCommand $instancename
}

function setupPollingTentacle {
    instancename=$1
    displayname=$instancename
    logpath="/etc/octopus"
    applicationpath="/home/Octopus/Applications"
    space="Default"
    envstring=""
    rolesstring=""
    workerpoolsstring=""

    read -p "Where would you like Tentacle to store log files? ($logpath):" inputlogpath
    logpath=$(assignNonEmptyValue $inputlogpath $logpath)
    
    read -p "Where would you like Tentacle to install appications to? ($applicationpath):" inputapplicationpath
    applicationpath=$(assignNonEmptyValue $inputapplicationpath $applicationpath)

    read -p "Octopus Server URL (eg. https://octopus-server):" octopusserverurl

    read -p 'Select auth method: 1) API-Key or 2) Username and Password (default 1): ' authmethod

    case $authmethod in
        2)
            read -p 'Username: ' username
            read -p 'Password: ' password
            auth="--username \"$username\" --password \"$password\""
            ;; 
        *)
            read -p 'API-Key: ' apikey
            auth="--apiKey \"$apikey\""
            ;;
    esac

    read -p 'Select type of Tentacle do you want to setup: 1) Deployment Target or 2) Worker (default 1): ' machinetype

    read -p 'What Space would you like to register this Tentacle in? (Default): ' spaceinput
    space=$(assignNonEmptyValue $spaceinput $space)

    read -p "What name would you like to register this Tentacle with? ($displayname): " displaynameinput
    displayname=$(assignNonEmptyValue $displaynameinput $displayname)

    case $machinetype in
        2)
            #Get worker pools
            while [ -z "$workerpoolsinput" ] 
            do
                read -p 'Enter the environments for this Tenteacle (comma seperated): ' workerpoolsinput
            done 
            workerpoolsstring=$(splitAndGetArgs "workerpool" "$workerpoolsinput")
            ;; 
        *)
            #Get environments
            while [ -z "$environmentsinput" ] 
            do
                read -p 'Enter the environments for this Tenteacle (comma seperated): ' environmentsinput
            done 
            envstring=$(splitAndGetArgs "environment" "$environmentsinput")

            #Get roles
            while [ -z "$rolesinput" ] 
            do
                read -p 'Enter the roles for this Tenteacle (comma seperated): ' rolesinput
            done
            rolesstring=$(splitAndGetArgs "role" "$rolesinput")
            ;;
    esac

    GREEN='\033[1;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m' # No Color

    echo -e "${GREEN}The following configuration commands will be run to configure Tentacle:"
    echo -e "${YELLOW}sudo /opt/octopus/tentacle/Tentacle create-instance --instance \"$instancename\" --config \"$logpath/$instancename/tentacle-$instancename.config\""
    echo -e "sudo /opt/octopus/tentacle/Tentacle new-certificate --instance \"$instancename\" --if-blank"
    echo -e "sudo /opt/octopus/tentacle/Tentacle configure --instance \"$instancename\" --app \"$applicationpath\" --noListen \"True\" --reset-trust"

    if [ $machinetype = 1 ]; then
        echo -e "sudo /opt/octopus/tentacle/Tentacle register-with --instance \"$instancename\" --server \"$octopusserverurl\" --name \"$displayname\" --comms-style \"TentacleActive\" --server-comms-port \"10943\" $auth --space \"$space\" $envstring $rolesstring --policy \"Default Machine Policy\"${NC}"
    else
        echo -e "sudo /opt/octopus/tentacle/Tentacle register-worker --instance \"$instancename\" --server \"$octopusserverurl\" --name \"$displayname\" --comms-style \"TentacleActive\" --server-comms-port \"10943\" $auth --space \"$space\" $workerpoolsstring --policy \"Default Machine Policy\"${NC}"
    fi

    read -p "Press enter to continue..."
    
    sudo /opt/octopus/tentacle/Tentacle create-instance --instance "$instancename" --config "$logpath/$instancename/tentacle-$instancename.config"
    sudo /opt/octopus/tentacle/Tentacle new-certificate --instance "$instancename" --if-blank
    sudo /opt/octopus/tentacle/Tentacle configure --instance "$instancename" --app "$applicationpath" --noListen "True" --reset-trust
    if [ $machinetype = 1 ]; then
        sudo /opt/octopus/tentacle/Tentacle register-with --instance "$instancename" --server "$octopusserverurl" --name "$displayname" --comms-style "TentacleActive" --server-comms-port "10943" $auth --space "$space" $envstring $rolesstring --policy "Default Machine Policy"
    else
        sudo /opt/octopus/tentacle/Tentacle register-worker --instance "$instancename" --server "$octopusserverurl" --name "$displayname" --comms-style "TentacleActive" --server-comms-port "10943" $auth --space "$space" $workerpoolsstring --policy "Default Machine Policy"
    fi

    showRunCommand $instancename
}

#Check to ensure dotnet core is installed

command -v dotnet >/dev/null 2>&1 || 
{ 
    echo "Please install dotnet core 2.2 to run Tentacle." >&2; 
    exit 1;     
}

if ! dotnet --list-sdks | grep -q "^2\.2" && ! dotnet --list-runtimes | grep -q "2\.2"; then
        echo "Please install dotnet core 2.2 to run Tentacle."
        exit 1
fi


while [ -z "$instance" ] 
do
    read -p 'Name of Tentacle instance: ' instance
done 

read -p 'What kind of Tentacle would you like to configure: 1) Listening or 2) Polling (default 1): ' commsstlye

case $commsstlye in
     2)
          setupPollingTentacle $instance
          ;; 
     *)
          setupListeningTentacle $instance
          ;;
esac