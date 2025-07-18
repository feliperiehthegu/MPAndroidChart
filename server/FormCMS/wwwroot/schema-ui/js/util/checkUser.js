import {getUserInfo} from "../services/services.js"
export function checkUser(onSuccess){
    getUserInfo().then(({_,error})=>
    {
        if (error){
            window.location.href = "/admin?ref=" + encodeURIComponent(window.location.href);
        }else {
            onSuccess();
        }
    });
}