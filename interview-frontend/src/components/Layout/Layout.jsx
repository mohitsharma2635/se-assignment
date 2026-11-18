import React from "react";
import Navbar from "./Navbar/Navbar";

const Layout = ({ children }) => {
    return (
        <div className="App">
            <Navbar />
            {children}
            <footer className="text-center text-muted small py-3">
                Fixed by{" "}
                <a href="https://mohitsharma.in" target="_blank" rel="noopener noreferrer">
                    mohitsharma.in
                </a>
            </footer>
        </div>
    );
};

export default Layout;
