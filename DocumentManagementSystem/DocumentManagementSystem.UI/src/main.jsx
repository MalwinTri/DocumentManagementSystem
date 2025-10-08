import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.jsx";
import "./index.css";
import { ConfirmProvider } from "@/components/ui/confirmDialog"; 

ReactDOM.createRoot(document.getElementById("root")).render(
    <React.StrictMode>
        <ConfirmProvider>
            <App />
        </ConfirmProvider>
    </React.StrictMode>
);
